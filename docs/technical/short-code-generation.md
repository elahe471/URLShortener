# Short-code generation

Sources: [MongoSequenceGenerator](../../Src/Shortener.API/Services/MongoSequenceGenerator.cs) and [ShortCodeGenerator](../../Src/Shortener.API/Services/ShortCodeGenerator.cs).

## Pipeline

```text
Atomic MongoDB increment -> positive sequence -> keyed 52-bit permutation -> Base62 -> nine characters
```

The design separates uniqueness from appearance. MongoDB allocates distinct input numbers; the keyed permutation changes their externally visible order.

## Atomic sequence allocation

The driver accesses the `Sequences` collection and selects the document with `_id = "short-url"`. `FindOneAndUpdateAsync` applies `$inc` to `Value`, sets `IsUpsert = true`, and requests `ReturnDocument.After`.

For a new counter, the first allocated value is 1. All service replicas must share this same allocation state. Concurrent behavior, including initialization of the counter, should be verified against a real MongoDB deployment.

Sequence allocation and URL insertion are separate writes. If insertion fails after allocation, the sequence is consumed. Gaps are acceptable because the algorithm requires uniqueness, not contiguous numbering. There is no transaction spanning both writes.

## Feistel permutation

The generator accepts sequences from `1` through `2^52 - 1`. It splits each value into two 26-bit halves and executes eight rounds:

```text
nextLeft  = right
nextRight = (left XOR F(right, round)) AND ((1 << 26) - 1)
```

The round input consists of the right half encoded as an unsigned 64-bit big-endian integer, followed by one round byte. `F` computes HMAC-SHA256 using the configured secret, reads the first four hash bytes as a big-endian unsigned integer, and retains 26 bits.

Each Feistel round is reversible, so the full transformation is a permutation of the 52-bit space. For a fixed key, distinct valid sequences have distinct permuted values. The permutation can produce zero; zero is a valid encoded output even though input sequence zero is rejected.

This is a custom obfuscation construction, not an authorization mechanism or a claim of independently audited cryptographic security. Possession of an active code is sufficient to access its redirect through the current public endpoint.

## Base62 encoding

The alphabet is:

```text
0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz
```

The generator fills a nine-character buffer from right to left using repeated division by 62. Unused leading positions become `0` characters. Since `62^9` exceeds `2^52`, every output fits without truncation.

The configured input capacity is **4,503,599,627,370,495** positive sequence values. A nonpositive input throws `ArgumentOutOfRangeException`; exceeding capacity throws `InvalidOperationException`. These guards protect the generator, but there is no dedicated HTTP capacity-exhaustion result.

## Uniqueness assumptions

Uniqueness depends on all of these conditions:

- A common, monotonically advancing sequence source for writers targeting the same URL collection.
- A stable secret key across those writers and across the lifetime of existing codes.
- Inputs remaining within the supported 52-bit range.
- Preservation of sequence state during backup, restore, and environment migration.

The MongoDB unique index provides a final persistence constraint. There is no query-before-insert collision check and no collision retry loop.

Changing the secret defines a different permutation. New inputs under the new key can map to codes previously produced under the old key. Existing redirects continue to use stored codes and do not require the key, but new-code uniqueness across key versions is no longer mathematically guaranteed. Key rotation therefore needs an explicit migration or namespace strategy.

The global counter adds one database write per creation and is shared by all writers. Redirect traffic does not touch it. A different allocation scheme should only be considered when creation throughput demonstrates that this counter is a bottleneck.
