# SaltMiner Project - AI Assistant Instructions

## Project Overview

SaltMiner is a source-available vulnerability and asset management platform that indexes security data into Elasticsearch. The codebase consists of multiple .NET solutions organized by functional area.

## Architecture

### Multi-Solution Structure

- **Saltworks.SaltMiner.Core** - Core business logic and domain models
- **Saltworks.SaltMiner.DataApi** - REST API for data operations (port 5000)
- **Saltworks.SaltMiner.Ui** - Vue.js frontend application
- **Saltworks.SaltMiner.Ui.Api** - REST API for UI operations (port 5001)
- **Saltworks.SaltMiner.DataClient** - Client library for DataApi consumption
- **Saltworks.SaltMiner.ElasticClient** - Elasticsearch integration layer
- **Saltworks.SaltMiner.SourceAdapters** - Integrations with external vulnerability scanners
- **Saltworks.SaltMiner.SyncAgent** - Background service for data synchronization
- **Saltworks.SaltMiner.Python** - Python utilities and scripts

### Data Flow Architecture

```
Ui (Vue.js)
  ↓
UiApi

Applications, UiApi
  ↓
DataClient
  ↓
DataApi (Controllers)
  ↓
DataApi (Contexts)
  ↓
ElasticClient
  ↓
Elasticsearch
```

## AI-Focused Documentation

The `ai/` folder contains debugging and testing guides specifically for AI agents:

### Debugging Guides

- **[ai/architecture.md](../ai/architecture.md)** - Application architecture, data flow layers, and key debugging principles. **Start here** when encountering issues.
- **[ai/dotnet-debug.md](../ai/dotnet-debug.md)** - How to debug .NET API endpoints using automated testing scripts. Use when API layer is suspect.
- **[ai/api-integrated-testing.md](../ai/api-integrated-testing.md)** - Automated pattern for running integration tests with live API. Captures both API and test logs.
- **[ai/test-debugging.md](../ai/test-debugging.md)** - Unit and integration test debugging strategies. Includes test execution methodology and index management.
- **[ai/elasticsearch.md](../ai/elasticsearch.md)** - Direct Elasticsearch access patterns (AiHelper) for data verification when API returns unexpected results.

### Templates

- **[TemplateAiHelper.cs](../TemplateAiHelper.cs)** - Template for creating direct Elasticsearch diagnostic helpers in test projects
- **[ai/ai-run-test.ps1.template](../ai/ai-run-test.ps1.template)** - PowerShell template for integration test scripts that run API automatically

### Quick Reference Files

- **[debug-api.http](../debug-api.http)** - HTTP requests for manual API testing
- **[FAQ.MD](../FAQ.MD)** - Project licensing and common questions

## Code Conventions

- **.NET Version:** .NET 8.0
- **Solution Organization:** Each major component has its own .sln file
- **Configuration:** External config files in `C:\Source\saltMiner-internal\config\`
- **Environment Variables:** 
  - `SALTMINER_ENVIRONMENT=Local` for development
  - `SALTMINER_API_CONFIG_PATH` points to external configuration

## Workflow Guidelines for AI

### When Debugging Issues

1. **Check architecture first:** Review [ai/architecture.md](../ai/architecture.md) for data flow and layer responsibilities
2. **Identify the layer:** Determine if issue is in ElasticClient, DataApi, or DataClient layer
3. **Use appropriate debugging approach:**
   - API issues → [ai/dotnet-debug.md](../ai/dotnet-debug.md)
   - Integration tests → [ai/api-integrated-testing.md](../ai/api-integrated-testing.md)
   - Unit tests → [ai/test-debugging.md](../ai/test-debugging.md)
   - Data verification → [ai/elasticsearch.md](../ai/elasticsearch.md)

### When Running Tests

1. **Layer-by-layer validation:** Start with lowest layer (ElasticClient unit tests), work upward
2. **One test class at a time:** Execute all once to identify failures, then fix one class at a time
3. **Integration tests:** Use `ai-run-api-test.ps1` pattern to capture both test and API logs
4. **Scripts:** Files prefixed with `ai-` are AI automation utilities

### When Making Code Changes

1. Each solution should be buildable independently
2. Consider impact across data flow layers
3. Integration tests require live API and Elasticsearch connections
4. External configuration files are outside the repository

## Documentation Standards

1. **ai/ folder:** AI-focused documentation only. No user-facing instructions about IDE usage.
2. **Less is more:** Keep AI documentation concise and action-oriented.
3. **No new .md files:** Do not create new markdown files without explicit direction.

## Project Goals

SaltMiner aggregates vulnerability data from multiple sources (Qualys, Wiz, Oligoscan, etc.) into a unified Elasticsearch index, providing comprehensive security posture visibility through both APIs and a web UI.
