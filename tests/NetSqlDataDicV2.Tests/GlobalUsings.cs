// Test frameworks
global using Xunit;
global using Moq;
global using FluentAssertions;

// Entity Framework
global using Microsoft.EntityFrameworkCore;

// Core namespaces
global using DataDictionary.AspNetCore.Core.Configuration;
global using DataDictionary.AspNetCore.Core.Data;
global using DataDictionary.AspNetCore.Core.Entities;
global using DataDictionary.AspNetCore.Core.Exceptions;
global using DataDictionary.AspNetCore.Core.Helpers;
global using DataDictionary.AspNetCore.Core.Models;
global using DataDictionary.AspNetCore.Core.Models.Dto;
global using DataDictionary.AspNetCore.Core.Models.ViewModels;
global using DataDictionary.AspNetCore.Core.Services;
global using DataDictionary.AspNetCore.Core.Services.DbContextProviders;
global using DataDictionary.AspNetCore.Core.Services.Security;

// RCL namespaces (controllers, middleware)
global using DataDictionary.AspNetCore.Areas.DataDictionary.Controllers;
global using DataDictionary.AspNetCore.Middleware;

// Test helpers
global using NetSqlDataDicV2.Tests.TestHelpers;
