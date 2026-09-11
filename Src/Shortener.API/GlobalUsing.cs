//solution: Shortener
global using Shortener.API.Endpoints.Contracts;
global using Shortener.API.Endpoints;
global using Shortener.API.Infrastructure.Extensions;
global using Shortener.API.Infrastructure;
global using Shortener.API.Services;
global using Shortener.API.Services.Interface;
global using Shortener.API.Models;
global using Shortener.API.Infrastructure.Configurations;
global using Shortener.API.Errors;
global using Shortener.API.Exceptions;
global using Shortener.API.Errors.CustomModel;
global using Shortener.API.Observability;
global using Shortener.API.Services.DTOs;


//Nuget Packages
global using FluentValidation;
global using Microsoft.EntityFrameworkCore;
global using MongoDB.EntityFrameworkCore;
global using MongoDB.EntityFrameworkCore.Extensions;
global using MongoDB.Bson.Serialization.Attributes;
global using MongoDB.Driver;
global using Scalar.AspNetCore;
global using Microsoft.Extensions.Options;
global using System.Buffers.Binary;
global using System.Security.Cryptography;
global using System.Collections.Frozen;
global using Microsoft.AspNetCore.Diagnostics;
global using Microsoft.AspNetCore.Mvc;
global using OpenTelemetry.Metrics;
global using Microsoft.Extensions.Caching.Hybrid;
