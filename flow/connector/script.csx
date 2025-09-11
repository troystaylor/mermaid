// Mermaid ERD Processor Custom Code - Power Platform Compatible Version
// Complete implementation for parsing Mermaid diagrams and determining Dataverse table operations
// Fixed for Power Platform Custom Connector runtime environment
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        var context = this.Context;
        var httpMethod = context.Request.Method;
        var requestUri = context.Request.RequestUri;
        
        // Log operation for monitoring
        this.Context.Logger.LogInformation($"Executing operation: {httpMethod} {requestUri}");
        
        // Add correlation ID for tracking
        var correlationId = Guid.NewGuid().ToString();
        if (!context.Request.Headers.Contains("X-Correlation-ID"))
        {
            context.Request.Headers.Add("X-Correlation-ID", correlationId);
        }
        
        // Add user agent for analytics
        if (!context.Request.Headers.Contains("User-Agent"))
        {
            context.Request.Headers.Add("User-Agent", "PowerPlatform-MermaidERDProcessor/1.0");
        }
        
        // Intercept and process Mermaid parsing requests
        if (httpMethod == HttpMethod.Post && requestUri.ToString().Contains("/parse"))
        {
            return await ProcessMermaidParsingRequest(context, correlationId);
        }
        
        // Intercept and process Mermaid conversion/upsert requests
        if (httpMethod == HttpMethod.Post && requestUri.ToString().Contains("/convert"))
        {
            return await ProcessMermaidConversionRequest(context, correlationId);
        }
        
        // For other operations, execute normally with enhanced error handling
        return await ExecuteWithEnhancedErrorHandling(context, correlationId);
    }
    
    private async Task<HttpResponseMessage> ProcessMermaidParsingRequest(Microsoft.PowerPlatform.Connectors.CustomCode.CSharp.IScriptContext context, string correlationId)
    {
        try
        {
            // Read the request content
            var requestContent = await context.Request.Content.ReadAsStringAsync();
            var requestJson = JObject.Parse(requestContent);
            
            var mermaidContent = requestJson["mermaidContent"]?.ToString();
            var validateSyntax = requestJson["validateSyntax"]?.ToObject<bool>() ?? true;
            var detectCDM = requestJson["detectCDM"]?.ToObject<bool>() ?? true;
            
            if (string.IsNullOrEmpty(mermaidContent))
            {
                return CreateErrorResponse("InvalidRequest", "Mermaid content is required", 400, correlationId);
            }
            
            // Parse the Mermaid content (supports all diagram types)
            var parser = new UniversalMermaidParser();
            var parseResult = parser.ParseMermaidContent(mermaidContent, validateSyntax, detectCDM);
            
            // Create comprehensive response
            var response = new JObject
            {
                ["diagramType"] = parseResult.DiagramType,
                ["parsedContent"] = JObject.FromObject(parseResult.ParsedContent),
                ["validation"] = JObject.FromObject(parseResult.Validation),
                ["_metadata"] = new JObject
                {
                    ["processedAt"] = DateTime.UtcNow.ToString("O"),
                    ["correlationId"] = correlationId,
                    ["connector"] = "MermaidERDProcessor",
                    ["version"] = "1.0.0",
                    ["diagramType"] = parseResult.DiagramType,
                    ["validationStatus"] = parseResult.Validation.Status,
                    ["supportsDataverseConversion"] = parseResult.SupportsDataverseConversion
                }
            };
            
            this.Context.Logger.LogInformation($"Successfully parsed {parseResult.DiagramType} diagram");
            
            return CreateJsonResponse(response.ToString(), 200);
        }
        catch (Exception ex)
        {
            this.Context.Logger.LogError($"Error processing Mermaid parsing request: {ex.Message}");
            return CreateErrorResponse("ParseError", $"Failed to parse Mermaid content: {ex.Message}", 500, correlationId);
        }
    }
    
    private async Task<HttpResponseMessage> ProcessMermaidConversionRequest(Microsoft.PowerPlatform.Connectors.CustomCode.CSharp.IScriptContext context, string correlationId)
    {
        try
        {
            // Validate authorization for Dataverse operations
            if (!context.Request.Headers.Contains("Authorization"))
            {
                return CreateErrorResponse("Unauthorized", "Authorization token is required for Dataverse operations", 401, correlationId);
            }
            
            var authHeader = context.Request.Headers.GetValues("Authorization").FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return CreateErrorResponse("Unauthorized", "Valid Bearer token is required for Dataverse operations", 401, correlationId);
            }
            
            // Read the request content
            var requestContent = await context.Request.Content.ReadAsStringAsync();
            var requestJson = JObject.Parse(requestContent);
            
            var mermaidContent = requestJson["mermaidContent"]?.ToString();
            var entityPrefix = requestJson["entityPrefix"]?.ToString() ?? "mermaid_";
            var publisherPrefix = requestJson["publisherPrefix"]?.ToString() ?? "pub_";
            var createIfNotExists = requestJson["createIfNotExists"]?.ToObject<bool>() ?? true;
            var updateExisting = requestJson["updateExisting"]?.ToObject<bool>() ?? false;
            
            if (string.IsNullOrEmpty(mermaidContent))
            {
                return CreateErrorResponse("InvalidRequest", "Mermaid content is required", 400, correlationId);
            }
            
            // Parse the Mermaid content
            var parser = new UniversalMermaidParser();
            var parseResult = parser.ParseMermaidContent(mermaidContent, true, true);
            
            // Check if diagram supports Dataverse conversion
            if (!parseResult.SupportsDataverseConversion)
            {
                return CreateErrorResponse("UnsupportedDiagram", 
                    $"Diagram type '{parseResult.DiagramType}' does not support Dataverse conversion", 400, correlationId);
            }
            
            // Get Dataverse environment URL from request
            var dataverseEnvironment = requestJson["dataverseEnvironment"]?.ToString();
            if (string.IsNullOrEmpty(dataverseEnvironment))
            {
                return CreateErrorResponse("InvalidRequest", "Dataverse environment URL is required for conversion operations", 400, correlationId);
            }
            
            // Generate table operations plan
            var operations = DetermineTableOperations(parseResult, entityPrefix, publisherPrefix, createIfNotExists, updateExisting);
            
            // Execute actual Dataverse operations
            var executionResults = await ExecuteDataverseOperations(operations, dataverseEnvironment, authHeader, correlationId);
            
            // Create comprehensive response with actual results
            var response = new JObject
            {
                ["operations"] = JObject.FromObject(operations),
                ["executionResults"] = JObject.FromObject(executionResults),
                ["summary"] = new JObject
                {
                    ["tablesPlanned"] = operations.CreateOperations?.Count ?? 0,
                    ["tablesCreated"] = executionResults.TablesCreated?.Count ?? 0,
                    ["tablesUpdated"] = executionResults.TablesUpdated?.Count ?? 0,
                    ["tablesSkipped"] = executionResults.TablesSkipped?.Count ?? 0,
                    ["relationshipsCreated"] = executionResults.RelationshipsCreated?.Count ?? 0,
                    ["errors"] = executionResults.Errors?.Count ?? 0,
                    ["estimatedTimeMinutes"] = CalculateEstimatedTime(operations),
                    ["actualExecutionTimeSeconds"] = executionResults.ExecutionTimeSeconds
                },
                ["_metadata"] = new JObject
                {
                    ["processedAt"] = DateTime.UtcNow.ToString("O"),
                    ["correlationId"] = correlationId,
                    ["connector"] = "MermaidERDProcessor",
                    ["version"] = "1.0.0",
                    ["diagramType"] = parseResult.DiagramType,
                    ["entityPrefix"] = entityPrefix,
                    ["publisherPrefix"] = publisherPrefix
                }
            };
            
            this.Context.Logger.LogInformation($"Generated operations for {parseResult.DiagramType} diagram");
            
            return CreateJsonResponse(response.ToString(), 200);
        }
        catch (Exception ex)
        {
            this.Context.Logger.LogError($"Error processing Mermaid conversion request: {ex.Message}");
            return CreateErrorResponse("ConversionError", $"Failed to convert Mermaid content: {ex.Message}", 500, correlationId);
        }
    }
    
    private async Task<HttpResponseMessage> ExecuteWithEnhancedErrorHandling(Microsoft.PowerPlatform.Connectors.CustomCode.CSharp.IScriptContext context, string correlationId)
    {
        try
        {
            // For POST operations, ensure proper content type
            if (context.Request.Method == HttpMethod.Post && context.Request.Content != null)
            {
                context.Request.Content.Headers.ContentType = 
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            }
            
            // Execute the request
            var response = await this.Context.SendAsync(context.Request, this.CancellationToken).ConfigureAwait(false);
            
            // Enhanced error handling
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                this.Context.Logger.LogError($"Operation failed: {response.StatusCode} - {errorContent}");
                
                // Transform error response for better user experience
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorResponse = new JObject()
                    {
                        ["error"] = new JObject()
                        {
                            ["code"] = "InvalidRequest",
                            ["message"] = "The request is invalid or malformed. Please check your input parameters.",
                            ["details"] = errorContent,
                            ["timestamp"] = DateTime.UtcNow.ToString("O"),
                            ["correlationId"] = correlationId
                        }
                    };
                    
                    response.Content = CreateJsonContent(errorResponse.ToString());
                }
            }
            else
            {
                // Enhance successful responses
                var responseContent = await response.Content.ReadAsStringAsync();
                
                try
                {
                    var responseJson = JObject.Parse(responseContent);
                    
                    // Add metadata to successful responses
                    responseJson["_metadata"] = new JObject()
                    {
                        ["processedAt"] = DateTime.UtcNow.ToString("O"),
                        ["correlationId"] = correlationId,
                        ["connector"] = "MermaidERDProcessor",
                        ["version"] = "1.0.0"
                    };
                    
                    response.Content = CreateJsonContent(responseJson.ToString());
                }
                catch (JsonReaderException)
                {
                    // If response is not JSON, leave it as is
                    this.Context.Logger.LogInformation("Response is not JSON, leaving unchanged");
                }
            }
            
            // Add custom headers to response
            response.Headers.Add("X-Powered-By", "Power Platform Custom Connector");
            response.Headers.Add("X-Connector-Version", "1.0.0");
            
            return response;
        }
        catch (Exception ex)
        {
            this.Context.Logger.LogError($"Error in ExecuteWithEnhancedErrorHandling: {ex.Message}");
            return CreateErrorResponse("InternalError", "An internal error occurred", 500, correlationId);
        }
    }
    
    private TableOperationsResult DetermineTableOperations(UniversalMermaidParseResult parseResult, 
        string entityPrefix, string publisherPrefix, bool createIfNotExists, bool updateExisting)
    {
        var operations = new TableOperationsResult
        {
            CreateOperations = new List<TableCreateOperation>(),
            UpdateOperations = new List<TableUpdateOperation>(),
            RelationshipOperations = new List<RelationshipCreateOperation>(),
            CreateIfNotExists = createIfNotExists,
            UpdateExisting = updateExisting
        };
        
        // Process based on diagram type
        switch (parseResult.DiagramType)
        {
            case "erDiagram":
                ProcessERDiagramForTables(parseResult, operations, entityPrefix, publisherPrefix, createIfNotExists, updateExisting);
                break;
            case "classDiagram":
                ProcessClassDiagramForTables(parseResult, operations, entityPrefix, publisherPrefix, createIfNotExists, updateExisting);
                break;
            default:
                // For other diagram types, try to extract entity-like structures
                ProcessGenericDiagramForTables(parseResult, operations, entityPrefix, publisherPrefix, createIfNotExists, updateExisting);
                break;
        }
        
        return operations;
    }
    
    private async Task<DataverseExecutionResult> ExecuteDataverseOperations(TableOperationsResult operations, 
        string dataverseEnvironment, string authHeader, string correlationId)
    {
        var startTime = DateTime.UtcNow;
        var result = new DataverseExecutionResult
        {
            TablesCreated = new List<string>(),
            TablesUpdated = new List<string>(),
            TablesSkipped = new List<string>(),
            RelationshipsCreated = new List<string>(),
            Errors = new List<ExecutionError>()
        };
        
        try
        {
            // Use Context.SendAsync as recommended for Power Platform Custom Connectors
            // Check existing tables first
            var existingTables = await GetExistingTables(dataverseEnvironment, authHeader, correlationId);
            
            // Process table creation/updates
            if (operations.CreateOperations != null)
            {
                foreach (var createOp in operations.CreateOperations)
                {
                    try
                    {
                        var tableExists = existingTables.ContainsKey(createOp.TableName.ToLower());
                        
                        if (tableExists)
                        {
                            this.Context.Logger.LogInformation($"Table {createOp.TableName} already exists");
                            // Handle existing table based on updateExisting flag
                            if (operations.UpdateExisting)
                            {
                                await UpdateExistingTable(dataverseEnvironment, authHeader, createOp, existingTables[createOp.TableName.ToLower()], correlationId);
                                result.TablesUpdated.Add(createOp.TableName);
                            }
                            else
                            {
                                result.TablesSkipped.Add(createOp.TableName);
                            }
                        }
                        else
                        {
                            // Create new table
                            if (operations.CreateIfNotExists)
                            {
                                await CreateNewTable(dataverseEnvironment, authHeader, createOp, correlationId);
                                result.TablesCreated.Add(createOp.TableName);
                                this.Context.Logger.LogInformation($"Created table {createOp.TableName}");
                            }
                            else
                            {
                                result.TablesSkipped.Add(createOp.TableName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new ExecutionError
                        {
                            Operation = $"Create/Update table {createOp.TableName}",
                            ErrorMessage = ex.Message,
                            ErrorType = "TableOperationError"
                        });
                        this.Context.Logger.LogError($"Error processing table {createOp.TableName}: {ex.Message}");
                    }
                }
            }
            
            // Process relationships (after tables are created)
            if (operations.RelationshipOperations != null)
            {
                foreach (var relOp in operations.RelationshipOperations)
                {
                    try
                    {
                        await CreateRelationship(dataverseEnvironment, authHeader, relOp, correlationId);
                        result.RelationshipsCreated.Add(relOp.RelationshipName);
                        this.Context.Logger.LogInformation($"Created relationship {relOp.RelationshipName}");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new ExecutionError
                        {
                            Operation = $"Create relationship {relOp.RelationshipName}",
                            ErrorMessage = ex.Message,
                            ErrorType = "RelationshipError"
                        });
                        this.Context.Logger.LogError($"Error creating relationship {relOp.RelationshipName}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ExecutionError
            {
                Operation = "Dataverse Operations",
                ErrorMessage = ex.Message,
                ErrorType = "GeneralError"
            });
            this.Context.Logger.LogError($"General error in Dataverse operations: {ex.Message}");
        }
        
        result.ExecutionTimeSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
        return result;
    }
    
    // Helper method to format operation instructions for manual execution
    private string FormatExecutionInstructions(TableOperationsResult operations)
    {
        var instructions = new StringBuilder();
        instructions.AppendLine("=== DATAVERSE OPERATION INSTRUCTIONS ===");
        instructions.AppendLine("Due to Power Platform Custom Connector limitations, execute these operations manually:");
        instructions.AppendLine();
        
        if (operations.CreateOperations?.Any() == true)
        {
            instructions.AppendLine("TABLES TO CREATE/UPDATE:");
            foreach (var createOp in operations.CreateOperations)
            {
                instructions.AppendLine($"- Table: {createOp.TableName} ({createOp.TableName})");
                instructions.AppendLine($"  Display Name: {createOp.DisplayName}");
                instructions.AppendLine($"  Description: {createOp.Description}");
                if (createOp.Attributes?.Any() == true)
                {
                    instructions.AppendLine("  Attributes:");
                    foreach (var attr in createOp.Attributes)
                    {
                        instructions.AppendLine($"    - {attr.SchemaName}: {attr.AttributeType} ({attr.DisplayName})");
                    }
                }
                instructions.AppendLine();
            }
        }
        
        if (operations.RelationshipOperations?.Any() == true)
        {
            instructions.AppendLine("RELATIONSHIPS TO CREATE:");
            foreach (var relOp in operations.RelationshipOperations)
            {
                instructions.AppendLine($"- {relOp.RelationshipName}: {relOp.FromTable} -> {relOp.ToTable}");
                instructions.AppendLine($"  Type: {relOp.RelationshipType}");
                instructions.AppendLine();
            }
        }
        
        return instructions.ToString();
    }
    
    // Helper methods for attribute processing
    private string SanitizeSchemaName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        
        // Remove invalid characters and ensure valid schema name
        var sanitized = System.Text.RegularExpressions.Regex.Replace(name.ToLower(), @"[^a-z0-9_]", "_");
        
        // Ensure it starts with a letter
        if (!char.IsLetter(sanitized[0]))
        {
            sanitized = "attr_" + sanitized;
        }
        
        return sanitized;
    }
    
    private async Task<Dictionary<string, string>> GetExistingTables(string dataverseEnvironment, string authHeader, string correlationId)
    {
        try
        {
            var requestUri = $"{dataverseEnvironment.TrimEnd('/')}/api/data/v9.2/EntityDefinitions?$select=LogicalName,MetadataId&$filter=IsManaged eq false";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("Authorization", authHeader);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("OData-MaxVersion", "4.0");
            request.Headers.Add("OData-Version", "4.0");
            
            var response = await this.Context.SendAsync(request, this.CancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(content);
            var entities = data["value"] as JArray;
            
            var existingTables = new Dictionary<string, string>();
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    var logicalName = entity["LogicalName"]?.ToString();
                    var metadataId = entity["MetadataId"]?.ToString();
                    if (!string.IsNullOrEmpty(logicalName) && !string.IsNullOrEmpty(metadataId))
                    {
                        existingTables[logicalName.ToLower()] = metadataId;
                    }
                }
            }
            
            this.Context.Logger.LogInformation($"Found {existingTables.Count} existing custom tables");
            return existingTables;
        }
        catch (Exception ex)
        {
            this.Context.Logger.LogError($"Error retrieving existing tables: {ex.Message}");
            return new Dictionary<string, string>();
        }
    }
    
    private async Task CreateNewTable(string dataverseEnvironment, string authHeader, TableCreateOperation createOp, string correlationId)
    {
        var entityPayload = new JObject
        {
            ["@odata.type"] = "Microsoft.Dynamics.CRM.EntityMetadata",
            ["LogicalName"] = createOp.TableName,
            ["DisplayName"] = new JObject
            {
                ["@odata.type"] = "Microsoft.Dynamics.CRM.Label",
                ["LocalizedLabels"] = new JArray
                {
                    new JObject
                    {
                        ["@odata.type"] = "Microsoft.Dynamics.CRM.LocalizedLabel",
                        ["Label"] = createOp.DisplayName,
                        ["LanguageCode"] = 1033
                    }
                }
            },
            ["DisplayCollectionName"] = new JObject
            {
                ["@odata.type"] = "Microsoft.Dynamics.CRM.Label",
                ["LocalizedLabels"] = new JArray
                {
                    new JObject
                    {
                        ["@odata.type"] = "Microsoft.Dynamics.CRM.LocalizedLabel",
                        ["Label"] = createOp.DisplayName + "s",
                        ["LanguageCode"] = 1033
                    }
                }
            },
            ["Description"] = new JObject
            {
                ["@odata.type"] = "Microsoft.Dynamics.CRM.Label",
                ["LocalizedLabels"] = new JArray
                {
                    new JObject
                    {
                        ["@odata.type"] = "Microsoft.Dynamics.CRM.LocalizedLabel",
                        ["Label"] = createOp.Description,
                        ["LanguageCode"] = 1033
                    }
                }
            },
            ["OwnershipType"] = "UserOwned",
            ["IsActivity"] = false,
            ["HasActivities"] = false,
            ["HasNotes"] = true,
            ["Attributes"] = new JArray()
        };
        
        // Add primary name attribute
        var primaryAttr = new JObject
        {
            ["@odata.type"] = "Microsoft.Dynamics.CRM.StringAttributeMetadata",
            ["AttributeType"] = "String",
            ["AttributeTypeName"] = new JObject { ["Value"] = "StringType" },
            ["LogicalName"] = createOp.PrimaryNameAttribute ?? "name",
            ["SchemaName"] = SanitizeSchemaName(createOp.PrimaryNameAttribute ?? "name"),
            ["IsPrimaryName"] = true,
            ["RequiredLevel"] = new JObject { ["Value"] = "SystemRequired" },
            ["MaxLength"] = 100,
            ["DisplayName"] = new JObject
            {
                ["@odata.type"] = "Microsoft.Dynamics.CRM.Label",
                ["LocalizedLabels"] = new JArray
                {
                    new JObject
                    {
                        ["@odata.type"] = "Microsoft.Dynamics.CRM.LocalizedLabel",
                        ["Label"] = "Name",
                        ["LanguageCode"] = 1033
                    }
                }
            }
        };
        
        var attributesArray = entityPayload["Attributes"] as JArray;
        attributesArray.Add(primaryAttr);
        
        // Add other attributes
        if (createOp.Attributes != null)
        {
            foreach (var attr in createOp.Attributes)
            {
                if (attr.SchemaName?.ToLower() != (createOp.PrimaryNameAttribute ?? "name").ToLower())
                {
                    var attrPayload = CreateAttributePayload(attr);
                    if (attrPayload != null)
                    {
                        attributesArray.Add(attrPayload);
                    }
                }
            }
        }
        
        var requestUri = $"{dataverseEnvironment.TrimEnd('/')}/api/data/v9.2/EntityDefinitions";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("Authorization", authHeader);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("OData-MaxVersion", "4.0");
        request.Headers.Add("OData-Version", "4.0");
        request.Content = new StringContent(entityPayload.ToString(), System.Text.Encoding.UTF8, "application/json");
        
        var response = await this.Context.SendAsync(request, this.CancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create table {createOp.TableName}: {response.StatusCode} - {errorContent}");
        }
    }
    
    private async Task UpdateExistingTable(string dataverseEnvironment, string authHeader, TableCreateOperation createOp, string metadataId, string correlationId)
    {
        // Add new attributes to existing table
        if (createOp.Attributes != null)
        {
            foreach (var attr in createOp.Attributes)
            {
                try
                {
                    var attrPayload = CreateAttributePayload(attr);
                    if (attrPayload != null)
                    {
                        var requestUri = $"{dataverseEnvironment.TrimEnd('/')}/api/data/v9.2/EntityDefinitions(LogicalName='{createOp.TableName}')/Attributes";
                        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                        request.Headers.Add("Authorization", authHeader);
                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Add("OData-MaxVersion", "4.0");
                        request.Headers.Add("OData-Version", "4.0");
                        request.Content = new StringContent(attrPayload.ToString(), System.Text.Encoding.UTF8, "application/json");
                        
                        var response = await this.Context.SendAsync(request, this.CancellationToken);
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                            this.Context.Logger.LogInformation($"Attribute {attr.SchemaName} already exists on table {createOp.TableName}");
                        }
                        else if (!response.IsSuccessStatusCode)
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            this.Context.Logger.LogWarning($"Failed to add attribute {attr.SchemaName} to table {createOp.TableName}: {response.StatusCode} - {errorContent}");
                        }
                        else
                        {
                            this.Context.Logger.LogInformation($"Added attribute {attr.SchemaName} to table {createOp.TableName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Context.Logger.LogWarning($"Error adding attribute {attr.SchemaName} to table {createOp.TableName}: {ex.Message}");
                }
            }
        }
    }
    
    private async Task CreateRelationship(string dataverseEnvironment, string authHeader, RelationshipCreateOperation relOp, string correlationId)
    {
        var relationshipPayload = new JObject
        {
            ["@odata.type"] = "Microsoft.Dynamics.CRM.OneToManyRelationshipMetadata",
            ["SchemaName"] = relOp.RelationshipName,
            ["ReferencedEntity"] = relOp.ToTable,
            ["ReferencingEntity"] = relOp.FromTable,
            ["Lookup"] = new JObject
            {
                ["@odata.type"] = "Microsoft.Dynamics.CRM.LookupAttributeMetadata",
                ["AttributeType"] = "Lookup",
                ["AttributeTypeName"] = new JObject { ["Value"] = "LookupType" },
                ["LogicalName"] = relOp.ForeignKeyName,
                ["SchemaName"] = SanitizeSchemaName(relOp.ForeignKeyName),
                ["DisplayName"] = new JObject
                {
                    ["@odata.type"] = "Microsoft.Dynamics.CRM.Label",
                    ["LocalizedLabels"] = new JArray
                    {
                        new JObject
                        {
                            ["@odata.type"] = "Microsoft.Dynamics.CRM.LocalizedLabel",
                            ["Label"] = relOp.ToTable.Replace("_", " "),
                            ["LanguageCode"] = 1033
                        }
                    }
                },
                ["RequiredLevel"] = new JObject { ["Value"] = "None" }
            }
        };
        
        var requestUri = $"{dataverseEnvironment.TrimEnd('/')}/api/data/v9.2/RelationshipDefinitions";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("Authorization", authHeader);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("OData-MaxVersion", "4.0");
        request.Headers.Add("OData-Version", "4.0");
        request.Content = new StringContent(relationshipPayload.ToString(), System.Text.Encoding.UTF8, "application/json");
        
        var response = await this.Context.SendAsync(request, this.CancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create relationship {relOp.RelationshipName}: {response.StatusCode} - {errorContent}");
        }
    }
    
    private JObject CreateAttributePayload(AttributeDefinition attr)
    {
        if (string.IsNullOrEmpty(attr.AttributeType)) return null;
        
        var basePayload = new JObject
        {
            ["LogicalName"] = attr.SchemaName,
            ["SchemaName"] = SanitizeSchemaName(attr.SchemaName),
            ["DisplayName"] = new JObject
            {
                ["@odata.type"] = "Microsoft.Dynamics.CRM.Label",
                ["LocalizedLabels"] = new JArray
                {
                    new JObject
                    {
                        ["@odata.type"] = "Microsoft.Dynamics.CRM.LocalizedLabel",
                        ["Label"] = attr.DisplayName,
                        ["LanguageCode"] = 1033
                    }
                }
            },
            ["RequiredLevel"] = new JObject { ["Value"] = attr.IsRequired ? "ApplicationRequired" : "None" }
        };
        
        // Set type-specific properties
        switch (attr.AttributeType.ToLower())
        {
            case "string":
            case "singlelineoftext":
                basePayload["@odata.type"] = "Microsoft.Dynamics.CRM.StringAttributeMetadata";
                basePayload["AttributeType"] = "String";
                basePayload["AttributeTypeName"] = new JObject { ["Value"] = "StringType" };
                basePayload["MaxLength"] = attr.MaxLength ?? 100;
                break;
            case "multilinetext":
                basePayload["@odata.type"] = "Microsoft.Dynamics.CRM.MemoAttributeMetadata";
                basePayload["AttributeType"] = "Memo";
                basePayload["AttributeTypeName"] = new JObject { ["Value"] = "MemoType" };
                basePayload["MaxLength"] = attr.MaxLength ?? 2000;
                break;
            case "integer":
            case "wholenumber":
                basePayload["@odata.type"] = "Microsoft.Dynamics.CRM.IntegerAttributeMetadata";
                basePayload["AttributeType"] = "Integer";
                basePayload["AttributeTypeName"] = new JObject { ["Value"] = "IntegerType" };
                basePayload["MinValue"] = -2147483648;
                basePayload["MaxValue"] = 2147483647;
                break;
            case "decimal":
            case "decimalnumber":
                basePayload["@odata.type"] = "Microsoft.Dynamics.CRM.DecimalAttributeMetadata";
                basePayload["AttributeType"] = "Decimal";
                basePayload["AttributeTypeName"] = new JObject { ["Value"] = "DecimalType" };
                basePayload["Precision"] = 2;
                break;
            case "boolean":
            case "twooptions":
                basePayload["@odata.type"] = "Microsoft.Dynamics.CRM.BooleanAttributeMetadata";
                basePayload["AttributeType"] = "Boolean";
                basePayload["AttributeTypeName"] = new JObject { ["Value"] = "BooleanType" };
                basePayload["OptionSet"] = new JObject
                {
                    ["@odata.type"] = "Microsoft.Dynamics.CRM.BooleanOptionSetMetadata",
                    ["TrueOption"] = new JObject { ["Value"] = 1, ["Label"] = new JObject { ["LocalizedLabels"] = new JArray { new JObject { ["Label"] = "Yes", ["LanguageCode"] = 1033 } } } },
                    ["FalseOption"] = new JObject { ["Value"] = 0, ["Label"] = new JObject { ["LocalizedLabels"] = new JArray { new JObject { ["Label"] = "No", ["LanguageCode"] = 1033 } } } }
                };
                break;
            case "datetime":
            case "dateandtime":
                basePayload["@odata.type"] = "Microsoft.Dynamics.CRM.DateTimeAttributeMetadata";
                basePayload["AttributeType"] = "DateTime";
                basePayload["AttributeTypeName"] = new JObject { ["Value"] = "DateTimeType" };
                basePayload["Format"] = "DateAndTime";
                break;
            default:
                // Default to string for unknown types
                basePayload["@odata.type"] = "Microsoft.Dynamics.CRM.StringAttributeMetadata";
                basePayload["AttributeType"] = "String";
                basePayload["AttributeTypeName"] = new JObject { ["Value"] = "StringType" };
                basePayload["MaxLength"] = 100;
                break;
        }
        
        return basePayload;
    }
    
    private void ProcessERDiagramForTables(UniversalMermaidParseResult parseResult, TableOperationsResult operations, 
        string entityPrefix, string publisherPrefix, bool createIfNotExists, bool updateExisting)
    {
        var parsedContent = parseResult.ParsedContent as ERDiagramContent;
        if (parsedContent?.Entities == null) return;
        
        foreach (var entity in parsedContent.Entities)
        {
            var tableName = $"{entityPrefix}{entity.Name}";
            var createOp = new TableCreateOperation
            {
                TableName = tableName,
                DisplayName = entity.Name,
                Description = $"Table generated from Mermaid ERD: {entity.Name}",
                PrimaryNameAttribute = "name",
                Attributes = new List<AttributeDefinition>()
            };
            
            // Add primary name attribute
            createOp.Attributes.Add(new AttributeDefinition
            {
                SchemaName = "name",
                DisplayName = "Name",
                AttributeType = "string",
                MaxLength = 100,
                IsRequired = true,
                IsPrimary = false
            });
            
            // Process entity attributes
            if (entity.Attributes != null)
            {
                foreach (var attr in entity.Attributes)
                {
                    var attrDef = new AttributeDefinition
                    {
                        SchemaName = SanitizeSchemaName(attr.Name),
                        DisplayName = attr.Name,
                        AttributeType = MapToDataverseType(attr.Type),
                        MaxLength = GetMaxLength(MapToDataverseType(attr.Type)),
                        IsRequired = attr.IsKey || attr.IsRequired,
                        IsPrimary = attr.IsKey
                    };
                    
                    // Validate the attribute
                    var validation = ValidateDataverseAttribute(attrDef);
                    if (validation.Status == "error")
                    {
                        // Skip attributes with blocking errors, log warning
                        this.Context.Logger.LogWarning($"Skipping attribute {attr.Name} due to validation errors");
                        continue;
                    }
                    
                    createOp.Attributes.Add(attrDef);
                }
            }
            
            operations.CreateOperations.Add(createOp);
        }
        
        // Process relationships
        if (parsedContent.Relationships != null)
        {
            foreach (var rel in parsedContent.Relationships)
            {
                var relOp = new RelationshipCreateOperation
                {
                    RelationshipName = $"{entityPrefix}rel_{rel.FromEntity}_{rel.ToEntity}",
                    FromTable = $"{entityPrefix}{rel.FromEntity}",
                    ToTable = $"{entityPrefix}{rel.ToEntity}",
                    RelationshipType = MapRelationshipType(rel.Cardinality),
                    ForeignKeyName = $"{rel.ToEntity.ToLower()}id"
                };
                
                operations.RelationshipOperations.Add(relOp);
            }
        }
    }
    
    private void ProcessClassDiagramForTables(UniversalMermaidParseResult parseResult, TableOperationsResult operations, 
        string entityPrefix, string publisherPrefix, bool createIfNotExists, bool updateExisting)
    {
        var parsedContent = parseResult.ParsedContent as ClassDiagramContent;
        if (parsedContent?.Classes == null) return;
        
        foreach (var cls in parsedContent.Classes)
        {
            var tableName = $"{entityPrefix}{cls.Name}";
            var createOp = new TableCreateOperation
            {
                TableName = tableName,
                DisplayName = cls.Name,
                Description = $"Table generated from Mermaid Class Diagram: {cls.Name}",
                PrimaryNameAttribute = "name",
                Attributes = new List<AttributeDefinition>()
            };
            
            // Add primary name attribute
            createOp.Attributes.Add(new AttributeDefinition
            {
                SchemaName = "name",
                DisplayName = "Name",
                AttributeType = "string",
                MaxLength = 100,
                IsRequired = true,
                IsPrimary = false
            });
            
            // Process class properties as attributes
            if (cls.Properties != null)
            {
                foreach (var prop in cls.Properties)
                {
                    var attrDef = new AttributeDefinition
                    {
                        SchemaName = SanitizeSchemaName(prop.Name),
                        DisplayName = prop.Name,
                        AttributeType = MapToDataverseType(prop.Type),
                        MaxLength = GetMaxLength(MapToDataverseType(prop.Type)),
                        IsRequired = false,
                        IsPrimary = false
                    };
                    
                    // Validate the attribute
                    var validation = ValidateDataverseAttribute(attrDef);
                    if (validation.Status == "error")
                    {
                        // Skip attributes with blocking errors, log warning
                        this.Context.Logger.LogWarning($"Skipping property {prop.Name} due to validation errors");
                        continue;
                    }
                    
                    createOp.Attributes.Add(attrDef);
                }
            }
            
            operations.CreateOperations.Add(createOp);
        }
    }
    
    private void ProcessGenericDiagramForTables(UniversalMermaidParseResult parseResult, TableOperationsResult operations, 
        string entityPrefix, string publisherPrefix, bool createIfNotExists, bool updateExisting)
    {
        // For other diagram types, create basic tracking tables based on nodes/elements
        var genericContent = parseResult.ParsedContent as GenericDiagramContent;
        if (genericContent?.Elements == null) return;
        
        // Group elements by type and create tables
        var elementGroups = genericContent.Elements.GroupBy(e => e.Type ?? "element");
        
        foreach (var group in elementGroups)
        {
            var tableName = $"{entityPrefix}{group.Key}";
            var createOp = new TableCreateOperation
            {
                TableName = tableName,
                DisplayName = group.Key,
                Description = $"Table generated from Mermaid {parseResult.DiagramType}: {group.Key}",
                PrimaryNameAttribute = "name",
                Attributes = new List<AttributeDefinition>()
            };
            
            // Add standard attributes
            createOp.Attributes.AddRange(new[]
            {
                new AttributeDefinition
                {
                    SchemaName = "name",
                    DisplayName = "Name",
                    AttributeType = "string",
                    MaxLength = 100,
                    IsRequired = true,
                    IsPrimary = false
                },
                new AttributeDefinition
                {
                    SchemaName = "description",
                    DisplayName = "Description",
                    AttributeType = "memo",
                    MaxLength = 2000,
                    IsRequired = false,
                    IsPrimary = false
                }
            });
            
            operations.CreateOperations.Add(createOp);
        }
    }
    
    private string MapDataType(string type)
    {
        return MapToDataverseType(type);
    }
    
    private string MapToDataverseType(string inputType)
    {
        if (string.IsNullOrEmpty(inputType)) return "SingleLineOfText";
        
        return inputType.ToLowerInvariant() switch
        {
            // Text Types
            "string" or "text" or "varchar" or "char" or "nvarchar" => "SingleLineOfText",
            "memo" or "longtext" or "multiline" or "textarea" => "MultilineText",
            "email" or "emailaddress" => "Email",
            "phone" or "phonenumber" or "telephone" => "Phone",
            "url" or "website" or "link" => "URL",
            "ticker" or "symbol" => "TickerSymbol",
            
            // Number Types
            "int" or "integer" or "whole" or "wholenumber" => "WholeNumber",
            "decimal" or "decimalnumber" => "DecimalNumber",
            "float" or "double" or "floatingpoint" or "floatingpointnumber" => "FloatingPointNumber",
            "money" or "currency" => "Currency",
            "bigint" or "long" => "BigInt",
            
            // Date/Time Types
            "datetime" or "timestamp" or "dateandtime" => "DateAndTime",
            "date" or "dateonly" => "DateOnly",
            "duration" or "timespan" => "Duration",
            "timezone" => "Timezone",
            "language" => "Language",
            
            // Boolean and Choice Types
            "bool" or "boolean" or "yesno" or "twooptions" => "TwoOptions",
            "choice" or "optionset" or "picklist" => "Choice",
            "choices" or "multiselectchoice" or "multiselect" => "Choices",
            
            // Lookup and Reference Types
            "lookup" or "reference" => "Lookup",
            "customer" => "Customer",
            "owner" => "Owner",
            
            // System Types
            "guid" or "uuid" or "uniqueidentifier" or "primarykey" => "UniqueIdentifier",
            "status" or "state" => "Status",
            "statusreason" => "StatusReason",
            
            // File and Media Types
            "file" or "attachment" => "File",
            "image" or "picture" => "Image",
            
            // Formula and Advanced Types
            "formula" or "calculated" => "Formula",
            "prompt" or "ai" => "Prompt",
            
            // Default fallback
            _ => "SingleLineOfText"
        };
    }
    
    private bool IsValidDataverseType(string dataType)
    {
        var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Text Types
            "SingleLineOfText", "MultilineText", "Email", "Phone", "URL", "TickerSymbol",
            
            // Number Types  
            "WholeNumber", "DecimalNumber", "FloatingPointNumber", "Currency", "BigInt",
            
            // Date/Time Types
            "DateAndTime", "DateOnly", "Duration", "Timezone", "Language",
            
            // Boolean and Choice Types
            "TwoOptions", "Choice", "Choices",
            
            // Lookup and Reference Types
            "Lookup", "Customer", "Owner",
            
            // System Types
            "UniqueIdentifier", "Status", "StatusReason",
            
            // File and Media Types
            "File", "Image",
            
            // Formula and Advanced Types
            "Formula", "Prompt"
        };
        
        return validTypes.Contains(dataType);
    }
    
    private int GetDecimalPrecision(string dataType)
    {
        return dataType?.ToLowerInvariant() switch
        {
            "decimalnumber" or "decimal" => 2,
            "floatingpointnumber" or "float" or "double" => 5,
            "currency" or "money" => 4,
            _ => 0
        };
    }
    
    private bool RequiresChoiceOptions(string dataType)
    {
        return dataType?.ToLowerInvariant() switch
        {
            "choice" or "optionset" or "picklist" => true,
            "choices" or "multiselectchoice" or "multiselect" => true,
            "status" or "state" => true,
            "statusreason" => true,
            _ => false
        };
    }
    
    private bool RequiresLookupTarget(string dataType)
    {
        return dataType?.ToLowerInvariant() switch
        {
            "lookup" or "reference" => true,
            "customer" => true,
            "owner" => true,
            _ => false
        };
    }
    
    private string GetDefaultChoiceOptions(string dataType)
    {
        return dataType?.ToLowerInvariant() switch
        {
            "twooptions" or "boolean" or "yesno" => "Yes,No",
            "status" or "state" => "Active,Inactive",
            _ => "Option1,Option2,Option3"
        };
    }
    
    private string GetLookupTargetTable(string dataType)
    {
        return dataType?.ToLowerInvariant() switch
        {
            "customer" => "account,contact",
            "owner" => "systemuser,team",
            _ => ""
        };
    }
    
    private bool IsSystemManagedType(string dataType)
    {
        return dataType?.ToLowerInvariant() switch
        {
            "uniqueidentifier" or "primarykey" => true,
            "status" or "state" => true,
            "statusreason" => true,
            "owner" => true,
            "bigint" or "timestamp" => true,
            _ => false
        };
    }
    
    private int GetMaxLength(string type)
    {
        if (string.IsNullOrEmpty(type)) return 100;
        
        return type.ToLowerInvariant() switch
        {
            // Text Types with specific limits
            "singlelineoftext" or "string" or "text" or "varchar" => 4000,
            "multilinetext" or "memo" or "longtext" => 1048576, // 1MB
            "email" => 100,
            "phone" or "phonenumber" => 50,
            "url" or "website" => 400,
            "tickersymbol" => 10,
            
            // Other types that don't use max length
            "wholenumber" or "int" or "integer" => 0,
            "decimalnumber" or "decimal" => 0,
            "floatingpointnumber" or "float" or "double" => 0,
            "currency" or "money" => 0,
            "bigint" or "long" => 0,
            "dateandtime" or "datetime" => 0,
            "dateonly" or "date" => 0,
            "twooptions" or "boolean" => 0,
            "choice" or "choices" => 0,
            "lookup" or "customer" or "owner" => 0,
            "uniqueidentifier" or "guid" => 0,
            "status" or "statusreason" => 0,
            "file" or "image" => 0,
            "formula" or "prompt" => 0,
            
            // Default fallback
            _ => 100
        };
    }
    
    private ValidationResult ValidateDataverseAttribute(AttributeDefinition attribute)
    {
        var result = new ValidationResult { Status = "valid", Issues = new List<ValidationIssue>() };
        
        // Validate data type
        if (!IsValidDataverseType(attribute.AttributeType))
        {
            result.Status = "error";
            result.Issues.Add(new ValidationIssue
            {
                Severity = "error",
                Message = $"Invalid data type '{attribute.AttributeType}' for attribute '{attribute.DisplayName}'",
                Line = 0
            });
        }
        
        // Validate max length for text types
        if (attribute.MaxLength > 0)
        {
            var maxAllowed = GetMaxLength(attribute.AttributeType);
            if (maxAllowed > 0 && attribute.MaxLength > maxAllowed)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Severity = "warning",
                    Message = $"Max length {attribute.MaxLength} exceeds limit of {maxAllowed} for type '{attribute.AttributeType}'",
                    Line = 0
                });
            }
        }
        
        // Validate choice types have options
        if (RequiresChoiceOptions(attribute.AttributeType))
        {
            result.Issues.Add(new ValidationIssue
            {
                Severity = "warning",
                Message = $"Choice type '{attribute.AttributeType}' requires option values to be defined",
                Line = 0
            });
        }
        
        // Validate lookup types have target
        if (RequiresLookupTarget(attribute.AttributeType))
        {
            result.Issues.Add(new ValidationIssue
            {
                Severity = "warning", 
                Message = $"Lookup type '{attribute.AttributeType}' requires target table to be specified",
                Line = 0
            });
        }
        
        // Validate logical name format
        if (!IsValidLogicalName(attribute.SchemaName))
        {
            result.Status = "error";
            result.Issues.Add(new ValidationIssue
            {
                Severity = "error",
                Message = $"Invalid schema name '{attribute.SchemaName}' for attribute '{attribute.DisplayName}'",
                Line = 0
            });
        }
        
        return result;
    }
    
    private bool IsValidLogicalName(string logicalName)
    {
        if (string.IsNullOrEmpty(logicalName)) return false;
        
        // Must start with letter or underscore, contain only letters, numbers, underscores
        return Regex.IsMatch(logicalName, @"^[a-zA-Z_][a-zA-Z0-9_]*$") && 
               logicalName.Length <= 100;
    }
    
    private string GenerateLogicalName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return "field";
        
        // Remove special characters and spaces, convert to lowercase
        var sanitized = Regex.Replace(displayName, @"[^a-zA-Z0-9_]", "").ToLower();
        
        // Ensure it starts with a letter
        if (!char.IsLetter(sanitized[0]))
        {
            sanitized = "field_" + sanitized;
        }
        
        // Ensure it's not too long
        if (sanitized.Length > 100)
        {
            sanitized = sanitized.Substring(0, 100);
        }
        
        return sanitized;
    }
    
    private string MapRelationshipType(Cardinality cardinality)
    {
        if (cardinality == null) return "one-to-many";
        
        var from = cardinality.From?.ToLower() ?? "";
        var to = cardinality.To?.ToLower() ?? "";
        
        if ((from.Contains("1") || from.Contains("one")) && (to.Contains("*") || to.Contains("many")))
            return "one-to-many";
        if ((from.Contains("*") || from.Contains("many")) && (to.Contains("*") || to.Contains("many")))
            return "many-to-many";
        if ((from.Contains("1") || from.Contains("one")) && (to.Contains("1") || to.Contains("one")))
            return "one-to-one";
        
        return "one-to-many";
    }
    
    private int CalculateEstimatedTime(TableOperationsResult operations)
    {
        var baseTime = 5; // Base 5 minutes
        var tableTime = (operations.CreateOperations?.Count ?? 0) * 2; // 2 minutes per table
        var relationshipTime = (operations.RelationshipOperations?.Count ?? 0) * 1; // 1 minute per relationship
        
        return baseTime + tableTime + relationshipTime;
    }
    
    private HttpResponseMessage CreateJsonResponse(string content, int statusCode)
    {
        var response = new HttpResponseMessage((System.Net.HttpStatusCode)statusCode);
        response.Content = CreateJsonContent(content);
        return response;
    }
    
    private StringContent CreateJsonContent(string content)
    {
        return new StringContent(content, Encoding.UTF8, "application/json");
    }
    
    private HttpResponseMessage CreateErrorResponse(string errorCode, string message, int statusCode, string correlationId)
    {
        var errorResponse = new JObject
        {
            ["error"] = new JObject
            {
                ["code"] = errorCode,
                ["message"] = message,
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["correlationId"] = correlationId
            }
        };
        
        return CreateJsonResponse(errorResponse.ToString(), statusCode);
    }
}

// Universal Mermaid Parser for all diagram types
public class UniversalMermaidParser
{
    private readonly Dictionary<string, string> cdmEntityMappings;
    
    public UniversalMermaidParser()
    {
        // Initialize CDM entity mappings
        cdmEntityMappings = InitializeCdmMappings();
    }
    
    public UniversalMermaidParseResult ParseMermaidContent(string mermaidContent, bool validateSyntax, bool detectCDM)
    {
        var result = new UniversalMermaidParseResult
        {
            DiagramType = DetectDiagramType(mermaidContent),
            ValidationMessages = new List<string>(),
            SupportsDataverseConversion = false
        };
        
        try
        {
            // Parse based on diagram type
            switch (result.DiagramType)
            {
                case "erDiagram":
                    result.ParsedContent = ParseERDiagram(mermaidContent);
                    result.SupportsDataverseConversion = true;
                    break;
                case "classDiagram":
                    result.ParsedContent = ParseClassDiagram(mermaidContent);
                    result.SupportsDataverseConversion = true;
                    break;
                case "flowchart":
                case "graph":
                    result.ParsedContent = ParseFlowchart(mermaidContent);
                    result.SupportsDataverseConversion = false;
                    break;
                case "sequenceDiagram":
                    result.ParsedContent = ParseSequenceDiagram(mermaidContent);
                    result.SupportsDataverseConversion = false;
                    break;
                default:
                    result.ParsedContent = ParseGenericDiagram(mermaidContent, result.DiagramType);
                    result.SupportsDataverseConversion = false;
                    break;
            }
            
            // Validate syntax if requested
            if (validateSyntax)
            {
                result.Validation = ValidateMermaidSyntax(mermaidContent, result.DiagramType);
            }
            else
            {
                result.Validation = new ValidationResult { Status = "skipped", Issues = new List<ValidationIssue>() };
            }
            
            // Detect CDM entities if requested and supported
            if (detectCDM && result.SupportsDataverseConversion)
            {
                result.CdmDetection = DetectCdmEntities(result.ParsedContent);
            }
            
        }
        catch (Exception ex)
        {
            result.ValidationMessages.Add($"Parse error: {ex.Message}");
            result.ParsedContent = new GenericDiagramContent { Elements = new List<DiagramElement>() };
        }
        
        return result;
    }
    
    private string DetectDiagramType(string content)
    {
        var lines = content.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
        
        foreach (var line in lines)
        {
            if (line.StartsWith("erDiagram")) return "erDiagram";
            if (line.StartsWith("classDiagram")) return "classDiagram";
            if (line.StartsWith("sequenceDiagram")) return "sequenceDiagram";
            if (line.StartsWith("stateDiagram")) return "stateDiagram";
            if (line.StartsWith("journey")) return "journey";
            if (line.StartsWith("gantt")) return "gantt";
            if (line.StartsWith("pie")) return "pie";
            if (line.StartsWith("quadrantChart")) return "quadrantChart";
            if (line.StartsWith("xyChart")) return "xyChart";
            if (line.StartsWith("mindmap")) return "mindmap";
            if (line.StartsWith("timeline")) return "timeline";
            if (line.StartsWith("sankey-beta")) return "sankey";
            if (line.StartsWith("block-beta")) return "block";
            if (line.StartsWith("gitgraph")) return "gitgraph";
            if (line.StartsWith("C4Context") || line.StartsWith("C4Container")) return "c4";
            if (line.StartsWith("requirementDiagram")) return "requirement";
            if (line.StartsWith("architecture-beta")) return "architecture";
            if (line.StartsWith("packet-beta")) return "packet";
            if (line.StartsWith("flowchart") || line.StartsWith("graph")) return "flowchart";
        }
        
        return "unknown";
    }
    
    private ERDiagramContent ParseERDiagram(string content)
    {
        var result = new ERDiagramContent
        {
            Entities = new List<Entity>(),
            Relationships = new List<Relationship>()
        };
        
        var lines = content.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
        Entity currentEntity = null;
        
        foreach (var line in lines)
        {
            if (line.StartsWith("erDiagram")) continue;
            
            // Entity definition
            if (Regex.IsMatch(line, @"^\w+\s*\{"))
            {
                var entityName = Regex.Match(line, @"^(\w+)").Groups[1].Value;
                currentEntity = new Entity
                {
                    Name = entityName,
                    Attributes = new List<EntityAttribute>()
                };
                result.Entities.Add(currentEntity);
            }
            // Entity attribute
            else if (currentEntity != null && line.Contains(" ") && !line.Contains("}"))
            {
                var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var attr = new EntityAttribute
                    {
                        Type = parts[0],
                        Name = parts[1],
                        IsKey = line.Contains("PK") || line.Contains("FK"),
                        IsRequired = !line.Contains("optional")
                    };
                    currentEntity.Attributes.Add(attr);
                }
            }
            // End entity
            else if (line == "}")
            {
                currentEntity = null;
            }
            // Relationship
            else if (line.Contains("||") || line.Contains("}|") || line.Contains("}{"))
            {
                var relationship = ParseRelationship(line);
                if (relationship != null)
                {
                    result.Relationships.Add(relationship);
                }
            }
        }
        
        return result;
    }
    
    private ClassDiagramContent ParseClassDiagram(string content)
    {
        var result = new ClassDiagramContent
        {
            Classes = new List<ClassDefinition>(),
            Relationships = new List<ClassRelationship>()
        };
        
        var lines = content.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
        
        foreach (var line in lines)
        {
            if (line.StartsWith("classDiagram")) continue;
            
            // Class definition with properties
            if (line.Contains("{") && line.Contains("}"))
            {
                var match = Regex.Match(line, @"class\s+(\w+)\s*\{([^}]*)\}");
                if (match.Success)
                {
                    var className = match.Groups[1].Value;
                    var propertiesText = match.Groups[2].Value;
                    
                    var cls = new ClassDefinition
                    {
                        Name = className,
                        Properties = new List<ClassProperty>(),
                        Methods = new List<ClassMethod>()
                    };
                    
                    // Parse properties
                    var properties = propertiesText.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var prop in properties)
                    {
                        var propTrimmed = prop.Trim();
                        if (!string.IsNullOrEmpty(propTrimmed))
                        {
                            var propParts = propTrimmed.Split(' ');
                            if (propParts.Length >= 2)
                            {
                                cls.Properties.Add(new ClassProperty
                                {
                                    Type = propParts[0],
                                    Name = propParts[1]
                                });
                            }
                        }
                    }
                    
                    result.Classes.Add(cls);
                }
            }
            // Simple class definition
            else if (line.StartsWith("class "))
            {
                var className = line.Substring(6).Trim();
                result.Classes.Add(new ClassDefinition
                {
                    Name = className,
                    Properties = new List<ClassProperty>(),
                    Methods = new List<ClassMethod>()
                });
            }
            // Relationship
            else if (line.Contains("-->") || line.Contains("--") || line.Contains("..>"))
            {
                var parts = Regex.Split(line, @"(-->|--|\.\.>)");
                if (parts.Length >= 3)
                {
                    result.Relationships.Add(new ClassRelationship
                    {
                        From = parts[0].Trim(),
                        To = parts[2].Trim(),
                        Type = parts[1].Trim()
                    });
                }
            }
        }
        
        return result;
    }
    
    private FlowchartContent ParseFlowchart(string content)
    {
        var result = new FlowchartContent
        {
            Nodes = new List<FlowchartNode>(),
            Connections = new List<FlowchartConnection>()
        };
        
        var lines = content.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
        
        foreach (var line in lines)
        {
            if (line.StartsWith("flowchart") || line.StartsWith("graph")) continue;
            
            // Node definition
            if (line.Contains("[") || line.Contains("(") || line.Contains("{"))
            {
                var nodeMatch = Regex.Match(line, @"(\w+)\s*[\[\(\{]([^\]\)\}]*)[\]\)\}]");
                if (nodeMatch.Success)
                {
                    result.Nodes.Add(new FlowchartNode
                    {
                        Id = nodeMatch.Groups[1].Value,
                        Label = nodeMatch.Groups[2].Value,
                        Shape = GetNodeShape(line)
                    });
                }
            }
            
            // Connection
            if (line.Contains("-->") || line.Contains("---"))
            {
                var connMatch = Regex.Match(line, @"(\w+)\s*(-->|---)\s*(\w+)");
                if (connMatch.Success)
                {
                    result.Connections.Add(new FlowchartConnection
                    {
                        From = connMatch.Groups[1].Value,
                        To = connMatch.Groups[3].Value,
                        Type = connMatch.Groups[2].Value
                    });
                }
            }
        }
        
        return result;
    }
    
    private SequenceDiagramContent ParseSequenceDiagram(string content)
    {
        var result = new SequenceDiagramContent
        {
            Participants = new List<string>(),
            Messages = new List<SequenceMessage>()
        };
        
        var lines = content.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
        
        foreach (var line in lines)
        {
            if (line.StartsWith("sequenceDiagram")) continue;
            
            // Participant
            if (line.StartsWith("participant "))
            {
                var participant = line.Substring(12).Trim();
                result.Participants.Add(participant);
            }
            // Message
            else if (line.Contains("->") || line.Contains("->>"))
            {
                var msgMatch = Regex.Match(line, @"(\w+)\s*(->>?)\s*(\w+)\s*:\s*(.*)");
                if (msgMatch.Success)
                {
                    result.Messages.Add(new SequenceMessage
                    {
                        From = msgMatch.Groups[1].Value,
                        To = msgMatch.Groups[3].Value,
                        Message = msgMatch.Groups[4].Value,
                        Type = msgMatch.Groups[2].Value
                    });
                }
            }
        }
        
        return result;
    }
    
    private GenericDiagramContent ParseGenericDiagram(string content, string diagramType)
    {
        var result = new GenericDiagramContent
        {
            Elements = new List<DiagramElement>()
        };
        
        var lines = content.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
        
        foreach (var line in lines)
        {
            if (line.StartsWith(diagramType)) continue;
            
            // Generic element detection
            if (!string.IsNullOrEmpty(line) && !line.StartsWith("%"))
            {
                result.Elements.Add(new DiagramElement
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "element",
                    Content = line,
                    Properties = new Dictionary<string, object>()
                });
            }
        }
        
        return result;
    }
    
    private Relationship ParseRelationship(string line)
    {
        var relationshipPattern = @"(\w+)\s*(\|\||}\||}{)\s*--\s*(\|\||}\||}{)\s*(\w+)\s*:\s*(.*)";
        var match = Regex.Match(line, relationshipPattern);
        
        if (match.Success)
        {
            return new Relationship
            {
                FromEntity = match.Groups[1].Value,
                ToEntity = match.Groups[4].Value,
                Label = match.Groups[5].Value,
                Cardinality = new Cardinality
                {
                    From = match.Groups[2].Value,
                    To = match.Groups[3].Value
                }
            };
        }
        
        return null;
    }
    
    private string GetNodeShape(string line)
    {
        if (line.Contains("[") && line.Contains("]")) return "rectangle";
        if (line.Contains("(") && line.Contains(")")) return "circle";
        if (line.Contains("{") && line.Contains("}")) return "rhombus";
        return "rectangle";
    }
    
    private ValidationResult ValidateMermaidSyntax(string content, string diagramType)
    {
        var result = new ValidationResult
        {
            Status = "valid",
            Issues = new List<ValidationIssue>()
        };
        
        // Basic validation checks
        var lines = content.Split('\n');
        
        // Check for diagram type declaration
        if (!lines.Any(l => l.Trim().StartsWith(diagramType)))
        {
            result.Issues.Add(new ValidationIssue
            {
                Severity = "warning",
                Message = $"Missing diagram type declaration: {diagramType}",
                Line = 1
            });
        }
        
        // Check for common syntax issues
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            // Check for unmatched brackets
            var openBrackets = line.Count(c => c == '{' || c == '[' || c == '(');
            var closeBrackets = line.Count(c => c == '}' || c == ']' || c == ')');
            
            if (openBrackets != closeBrackets)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Severity = "error",
                    Message = "Unmatched brackets",
                    Line = i + 1
                });
            }
        }
        
        if (result.Issues.Any(i => i.Severity == "error"))
        {
            result.Status = "error";
        }
        else if (result.Issues.Any(i => i.Severity == "warning"))
        {
            result.Status = "warning";
        }
        
        return result;
    }
    
    private CdmDetectionResult DetectCdmEntities(object parsedContent)
    {
        var result = new CdmDetectionResult
        {
            Matches = new List<CdmMatch>()
        };
        
        // Extract entity names based on content type
        var entityNames = new List<string>();
        
        if (parsedContent is ERDiagramContent erdContent)
        {
            entityNames = erdContent.Entities?.Select(e => e.Name).ToList() ?? new List<string>();
        }
        else if (parsedContent is ClassDiagramContent classContent)
        {
            entityNames = classContent.Classes?.Select(c => c.Name).ToList() ?? new List<string>();
        }
        
        // Check against CDM mappings
        foreach (var entityName in entityNames)
        {
            foreach (var mapping in cdmEntityMappings)
            {
                if (mapping.Key.Contains(entityName.ToLower()) || 
                    entityName.ToLower().Contains(mapping.Key))
                {
                    result.Matches.Add(new CdmMatch
                    {
                        EntityName = entityName,
                        CdmEntity = mapping.Value,
                        MatchType = "name",
                        Confidence = 0.8
                    });
                }
            }
        }
        
        return result;
    }
    
    private Dictionary<string, string> InitializeCdmMappings()
    {
        return new Dictionary<string, string>
        {
            // Core entities
            {"account", "Account"},
            {"contact", "Contact"},
            {"user", "SystemUser"},
            {"customer", "Customer"},
            {"lead", "Lead"},
            {"opportunity", "Opportunity"},
            {"case", "Incident"},
            {"ticket", "Incident"},
            {"product", "Product"},
            {"order", "SalesOrder"},
            {"invoice", "Invoice"},
            {"quote", "Quote"},
            {"campaign", "Campaign"},
            {"activity", "ActivityPointer"},
            {"task", "Task"},
            {"appointment", "Appointment"},
            {"email", "Email"},
            {"phonecall", "PhoneCall"},
            {"note", "Annotation"},
            {"team", "Team"},
            {"queue", "Queue"},
            {"territory", "Territory"},
            {"currency", "TransactionCurrency"},
            {"organization", "Organization"},
            {"businessunit", "BusinessUnit"}
        };
    }
}

// Data Models for Universal Mermaid Processing
public class UniversalMermaidParseResult
{
    public string DiagramType { get; set; }
    public object ParsedContent { get; set; }
    public ValidationResult Validation { get; set; }
    public CdmDetectionResult CdmDetection { get; set; }
    public List<string> ValidationMessages { get; set; }
    public bool SupportsDataverseConversion { get; set; }
}

public class ERDiagramContent
{
    public List<Entity> Entities { get; set; }
    public List<Relationship> Relationships { get; set; }
}

public class ClassDiagramContent
{
    public List<ClassDefinition> Classes { get; set; }
    public List<ClassRelationship> Relationships { get; set; }
}

public class FlowchartContent
{
    public List<FlowchartNode> Nodes { get; set; }
    public List<FlowchartConnection> Connections { get; set; }
}

public class SequenceDiagramContent
{
    public List<string> Participants { get; set; }
    public List<SequenceMessage> Messages { get; set; }
}

public class GenericDiagramContent
{
    public List<DiagramElement> Elements { get; set; }
}

public class Entity
{
    public string Name { get; set; }
    public List<EntityAttribute> Attributes { get; set; }
}

public class EntityAttribute
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool IsKey { get; set; }
    public bool IsRequired { get; set; }
}

public class Relationship
{
    public string FromEntity { get; set; }
    public string ToEntity { get; set; }
    public string Label { get; set; }
    public Cardinality Cardinality { get; set; }
}

public class Cardinality
{
    public string From { get; set; }
    public string To { get; set; }
}

public class ClassDefinition
{
    public string Name { get; set; }
    public List<ClassProperty> Properties { get; set; }
    public List<ClassMethod> Methods { get; set; }
}

public class ClassProperty
{
    public string Name { get; set; }
    public string Type { get; set; }
}

public class ClassMethod
{
    public string Name { get; set; }
    public string ReturnType { get; set; }
    public List<string> Parameters { get; set; }
}

public class ClassRelationship
{
    public string From { get; set; }
    public string To { get; set; }
    public string Type { get; set; }
}

public class FlowchartNode
{
    public string Id { get; set; }
    public string Label { get; set; }
    public string Shape { get; set; }
}

public class FlowchartConnection
{
    public string From { get; set; }
    public string To { get; set; }
    public string Type { get; set; }
}

public class SequenceMessage
{
    public string From { get; set; }
    public string To { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
}

public class DiagramElement
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Content { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}

public class ValidationResult
{
    public string Status { get; set; }
    public List<ValidationIssue> Issues { get; set; }
}

public class ValidationIssue
{
    public string Severity { get; set; }
    public string Message { get; set; }
    public int Line { get; set; }
}

public class CdmDetectionResult
{
    public List<CdmMatch> Matches { get; set; }
}

public class CdmMatch
{
    public string EntityName { get; set; }
    public string CdmEntity { get; set; }
    public string MatchType { get; set; }
    public double Confidence { get; set; }
}

public class TableOperationsResult
{
    public List<TableCreateOperation> CreateOperations { get; set; }
    public List<TableUpdateOperation> UpdateOperations { get; set; }
    public List<RelationshipCreateOperation> RelationshipOperations { get; set; }
    public bool CreateIfNotExists { get; set; }
    public bool UpdateExisting { get; set; }
}

public class TableCreateOperation
{
    public string TableName { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string PrimaryNameAttribute { get; set; }
    public List<AttributeDefinition> Attributes { get; set; }
}

public class TableUpdateOperation
{
    public string TableName { get; set; }
    public List<AttributeDefinition> AttributesToAdd { get; set; }
    public List<string> AttributesToRemove { get; set; }
}

public class RelationshipCreateOperation
{
    public string RelationshipName { get; set; }
    public string FromTable { get; set; }
    public string ToTable { get; set; }
    public string RelationshipType { get; set; }
    public string ForeignKeyName { get; set; }
}

public class AttributeDefinition
{
    public string SchemaName { get; set; }
    public string DisplayName { get; set; }
    public string AttributeType { get; set; }
    public int? MaxLength { get; set; }
    public bool IsRequired { get; set; }
    public bool IsPrimary { get; set; }
}

public class DataverseExecutionResult
{
    public List<string> TablesCreated { get; set; }
    public List<string> TablesUpdated { get; set; }
    public List<string> TablesSkipped { get; set; }
    public List<string> RelationshipsCreated { get; set; }
    public List<ExecutionError> Errors { get; set; }
    public double ExecutionTimeSeconds { get; set; }
}

public class ExecutionError
{
    public string Operation { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorType { get; set; }
}