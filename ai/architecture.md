# SaltMiner Application Architecture (AI Agent Guide)

## Data Call Flow Through Application Layers

When debugging data operations, understand how a request flows through the system:

```
Ui (vue.js)
  ↓ calls
Ui Api

application & Ui Api
  ↓ calls
DataClient method 
  ↓ calls
Data API *Controller action 
  ↓ calls
Data API *Context method 
  ↓ calls
ElasticClient method 
  ↓ calls
Elasticsearch .net library 
  ↓ calls
Elasticsearch
```

**Key Debugging Principle:** If ElasticClient layer tests pass but DataClient integration tests fail, the problem lies somewhere between the DataClient and ElasticClient layers (typically in the DataAPI Controller or Context).


## Documentation Standards

- Do NOT create new .md files to report on output or debugging results
- .md files should only be created per explicit work instructions
- Exception: Recommend updates to existing documentation (see ai folder) if infrastructure changes.
- Use inline code comments and TODO markers to document temporary changes

## Related Documentation

- [dotnet-debug.md](dotnet-debug.md) - How to debug .NET API endpoints
- [api-integrated-testing.md](api-integrated-testing.md) - Running integration tests with live API
- [test-debugging.md](test-debugging.md) - Unit and integration test debugging strategies
- [elasticsearch.md](elasticsearch.md) - Direct Elasticsearch access for data verification
