# Universal Mermaid Connector - Test Examples

This document provides comprehensive test examples for the Universal Mermaid Connector covering all supported operations, diagram types, and output formats.

## Quick Test Setup

**Connector ID**: `c81d60e2-e993-f011-b41b-000d3a365052`
**Base URL**: Use your Power Platform environment URL
**Operations**: ParseMermaidDiagram, ConvertMermaidToFormats, InvokeServer (MCP)

---

## 1. REST API Examples

### Example 1: Parse ERD Diagram
**Operation**: ParseMermaidDiagram
**Request Body**:
```json
{
  "mermaidContent": "erDiagram\n    CUSTOMER {\n        string name\n        string email\n        int customer_id PK\n    }\n    ORDER {\n        int order_id PK\n        string order_date\n        int customer_id FK\n    }\n    CUSTOMER ||--o{ ORDER : places"
}
```

**Expected Response**:
```json
{
  "success": true,
  "diagramType": "erDiagram",
  "entities": [
    {
      "name": "CUSTOMER",
      "logicalName": "customer",
      "entityType": "Entity",
      "attributes": [
        {"name": "name", "type": "string", "constraints": "", "isPrimaryKey": false, "isForeignKey": false},
        {"name": "email", "type": "string", "constraints": "", "isPrimaryKey": false, "isForeignKey": false},
        {"name": "customer_id", "type": "int", "constraints": "PK", "isPrimaryKey": true, "isForeignKey": false}
      ],
      "methods": [],
      "metadata": {}
    },
    {
      "name": "ORDER",
      "logicalName": "order",
      "entityType": "Entity",
      "attributes": [
        {"name": "order_id", "type": "int", "constraints": "PK", "isPrimaryKey": true, "isForeignKey": false},
        {"name": "order_date", "type": "string", "constraints": "", "isPrimaryKey": false, "isForeignKey": false},
        {"name": "customer_id", "type": "int", "constraints": "FK", "isPrimaryKey": false, "isForeignKey": true}
      ],
      "methods": [],
      "metadata": {}
    }
  ],
  "relationships": [
    {
      "from": "CUSTOMER",
      "to": "ORDER",
      "type": "one-to-many",
      "cardinality": "||--o{",
      "label": "places"
    }
  ],
  "validation": {
    "isValid": true,
    "errors": [],
    "warnings": []
  },
  "cdmDetection": {
    "detectedEntities": []
  }
}
```

### Example 2: Convert Class Diagram to Multiple Formats
**Operation**: ConvertMermaidToFormats
**Request Body**:
```json
{
  "entities": [
    {
      "name": "Animal",
      "logicalName": "animal",
      "attributes": [
        {"name": "name", "dataType": "String"},
        {"name": "age", "dataType": "int"}
      ]
    },
    {
      "name": "Dog", 
      "logicalName": "dog",
      "attributes": [
        {"name": "name", "dataType": "String"},
        {"name": "age", "dataType": "int"},
        {"name": "breed", "dataType": "String"}
      ]
    }
  ],
  "relationships": [
    {
      "from": "Animal",
      "to": "Dog", 
      "type": "inheritance"
    }
  ],
  "outputFormat": "all"
}
```

**Expected Response**:
```json
{
  "success": true,
  "conversions": {
    "jsonSchema": "{\n  \"$schema\": \"http://json-schema.org/draft-07/schema#\",\n  \"title\": \"Generated Schema\",\n  \"type\": \"object\",\n  \"definitions\": {\n    \"Animal\": {\n      \"type\": \"object\",\n      \"properties\": {\n        \"name\": {\"type\": \"string\", \"description\": \"name field of Animal\"},\n        \"age\": {\"type\": \"integer\", \"description\": \"age field of Animal\"}\n      }\n    },\n    \"Dog\": {\n      \"type\": \"object\",\n      \"properties\": {\n        \"name\": {\"type\": \"string\", \"description\": \"name field of Dog\"},\n        \"age\": {\"type\": \"integer\", \"description\": \"age field of Dog\"},\n        \"breed\": {\"type\": \"string\", \"description\": \"breed field of Dog\"}\n      }\n    }\n  }\n}",
    "sqlDDL": "-- Generated SQL DDL\n\nCREATE TABLE Animal (\n    name VARCHAR(255),\n    age INT\n);\n\nCREATE TABLE Dog (\n    name VARCHAR(255),\n    age INT,\n    breed VARCHAR(255)\n);",
    "typescript": "// Generated TypeScript\ninterface Animal {\n  name: string;\n  age: number;\n}\n\ninterface Dog extends Animal {\n  breed: string;\n}",
    "csharp": "// Generated C#\npublic class Animal {\n  public string name { get; set; }\n  public int age { get; set; }\n}\n\npublic class Dog : Animal {\n  public string breed { get; set; }\n}",
    "python": "# Generated Python\nfrom dataclasses import dataclass\n\n@dataclass\nclass Animal:\n  name: str\n  age: int\n\n@dataclass\nclass Dog(Animal):\n  breed: str",
    "java": "// Generated Java\npublic class Animal {\n    private String name;\n    private Integer age;\n}\n\npublic class Dog extends Animal {\n    private String breed;\n}",
    "go": "// Generated Go\npackage main\n\ntype Animal struct {\n    name string `json:\"name\"`\n    age int `json:\"age\"`\n}\n\ntype Dog struct {\n    name string `json:\"name\"`\n    age int `json:\"age\"`\n    breed string `json:\"breed\"`\n}",
    "rust": "// Generated Rust\nuse serde::{Deserialize, Serialize};\n\n#[derive(Debug, Serialize, Deserialize)]\npub struct Animal {\n    pub name: String,\n    pub age: i32,\n}\n\n#[derive(Debug, Serialize, Deserialize)]\npub struct Dog {\n    pub name: String,\n    pub age: i32,\n    pub breed: String,\n}",
    "graphql": "# Generated GraphQL\ntype Animal {\n  name: String\n  age: Int\n}\n\ntype Dog {\n  name: String\n  age: Int\n  breed: String\n}",
    "react": "// Generated React TypeScript\nimport React from 'react';\n\ninterface AnimalProps {\n  name: string;\n  age: number;\n}\n\ninterface DogProps {\n  name: string;\n  age: number;\n  breed: string;\n}",
    "vue": "// Generated Vue Composition API\nexport interface Animal {\n  name: string\n  age: number\n}\n\nexport interface Dog {\n  name: string\n  age: number\n  breed: string\n}",
    "swift": "// Generated Swift\nimport Foundation\n\nstruct Animal: Codable {\n    let name: String\n    let age: Int\n}\n\nstruct Dog: Codable {\n    let name: String\n    let age: Int\n    let breed: String\n}",
    "dataverseSchema": "// Generated Dataverse Schema\n// Dataverse schema generation not yet implemented",
    "powerAppsFormulas": "// Generated Power Apps Formulas\n// Power Apps formula generation not yet implemented",
    "postgresqlDDL": "-- Generated PostgreSQL DDL\n\nCREATE TABLE Animal (\n    name VARCHAR(255),\n    age INT\n);\n\nCREATE TABLE Dog (\n    name VARCHAR(255),\n    age INT,\n    breed VARCHAR(255)\n);",
    "mysqlDDL": "-- Generated MySQL DDL\n\nCREATE TABLE Animal (\n    name VARCHAR(255),\n    age INT\n);\n\nCREATE TABLE Dog (\n    name VARCHAR(255),\n    age INT,\n    breed VARCHAR(255)\n);",
    "mongooseSchema": "// Generated Mongoose Schema\nconst mongoose = require('mongoose');\n\nconst AnimalSchema = new mongoose.Schema({\n  name: String,\n  age: Number,\n});\n\nmodule.exports = mongoose.model('Animal', AnimalSchema);\n\nconst DogSchema = new mongoose.Schema({\n  name: String,\n  age: Number,\n  breed: String,\n});\n\nmodule.exports = mongoose.model('Dog', DogSchema);",
    "markdown": "# Entity Documentation\n\n## Animal\n\n| Field | Type |\n|-------|------|\n| name | String |\n| age | int |\n\n## Dog\n\n*Inherits from: Animal*\n\n| Field | Type |\n|-------|------|\n| name | String |\n| age | int |\n| breed | String |"
  }
}
```

### Example 3: Parse Flowchart
**Operation**: ParseMermaidDiagram
**Request Body**:
```json
{
  "mermaidContent": "flowchart TD\n    A[Start] --> B{Decision}\n    B -->|Yes| C[Process 1]\n    B -->|No| D[Process 2]\n    C --> E[End]\n    D --> E"
}
```

### Example 4: Parse Sequence Diagram
**Operation**: ParseMermaidDiagram
**Request Body**:
```json
{
  "mermaidContent": "sequenceDiagram\n    participant A as Alice\n    participant B as Bob\n    A->>B: Hello Bob, how are you?\n    B-->>A: Great!\n    A->>B: See you later!"
}
```

---

## 2. MCP Protocol Examples

### Example 1: Initialize MCP Server
**Operation**: InvokeServer
**Request Body**:
```json
{
  "jsonrpc": "2.0",
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "tools": {}
    },
    "clientInfo": {
      "name": "test-client",
      "version": "1.0.0"
    }
  },
  "id": "1"
}
```

### Example 2: List Available Tools
**Operation**: InvokeServer
**Request Body**:
```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "params": {},
  "id": "2"
}
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "result": {
    "tools": [
      {
        "name": "parse_mermaid_diagram",
        "description": "Parse Mermaid diagram and extract structured information"
      },
      {
        "name": "detect_diagram_type",
        "description": "Detect the type of Mermaid diagram with confidence scoring"
      },
      {
        "name": "extract_entities",
        "description": "Extract entities from diagrams with detailed field information"
      },
      {
        "name": "extract_relationships",
        "description": "Extract relationships between entities"
      },
      {
        "name": "convert_to_formats",
        "description": "Convert diagram to various output formats"
      },
      {
        "name": "validate_mermaid_syntax",
        "description": "Validate Mermaid diagram syntax"
      }
    ]
  }
}
```

### Example 3: Parse Diagram via MCP
**Operation**: InvokeServer
**Request Body**:
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "parse_mermaid_diagram",
    "arguments": {
      "content": "erDiagram\n    USER {\n        int id PK\n        string username\n        string email\n    }\n    POST {\n        int id PK\n        string title\n        string content\n        int user_id FK\n    }\n    USER ||--o{ POST : creates"
    }
  },
  "id": "3"
}
```

### Example 4: Detect Diagram Type via MCP
**Operation**: InvokeServer
**Request Body**:
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "detect_diagram_type",
    "arguments": {
      "content": "graph TD\n    A --> B\n    B --> C\n    C --> D"
    }
  },
  "id": "4"
}
```

### Example 5: Convert to Multiple Formats via MCP
**Operation**: InvokeServer
**Request Body**:
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "convert_to_formats",
    "arguments": {
      "content": "classDiagram\n    class Vehicle {\n        +String make\n        +String model\n        +start()\n    }",
      "format": ["typescript", "python", "plantuml"]
    }
  },
  "id": "5"
}
```

### Example 6: Validate Syntax via MCP
**Operation**: InvokeServer
**Request Body**:
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "validate_mermaid_syntax",
    "arguments": {
      "content": "erDiagram\n    CUSTOMER {\n        string name\n        int id\n    }\n    ORDER {\n        int order_id\n    }\n    CUSTOMER ||--o{ ORDER : has"
    }
  },
  "id": "6"
}
```

---

## 3. Advanced Diagram Type Examples

### State Diagram
```
stateDiagram-v2
    [*] --> Still
    Still --> [*]
    Still --> Moving
    Moving --> Still
    Moving --> Crash
    Crash --> [*]
```

### Gantt Chart
```
gantt
    title Project Timeline
    dateFormat YYYY-MM-DD
    section Development
    Task 1: 2024-01-01, 30d
    Task 2: after task1, 20d
```

### Journey Map
```
journey
    title My working day
    section Go to work
      Make tea: 5: Me
      Go upstairs: 3: Me
      Do work: 1: Me, Cat
    section Go home
      Go downstairs: 5: Me
      Sit down: 5: Me
```

### Git Graph
```
gitgraph
    commit id: "Initial"
    branch develop
    commit id: "Feature A"
    checkout main
    commit id: "Hotfix"
    checkout develop
    commit id: "Feature B"
    checkout main
    merge develop
```

### Pie Chart
```
pie title Pets adopted by volunteers
    "Dogs" : 386
    "Cats" : 85
    "Rats" : 15
```

### Quadrant Chart
```
quadrantChart
    title Reach and influence
    x-axis Low Reach --> High Reach
    y-axis Low Influence --> High Influence
    quadrant-1 We should expand
    quadrant-2 Need to promote
    quadrant-3 Re-evaluate
    quadrant-4 May be improved
```

---

## 4. Testing All Output Formats

### Available Output Formats
Test the ConvertMermaidToFormats operation with these formats:

1. **jsonSchema** - JSON Schema specification
2. **sqlDDL** - SQL Data Definition Language
3. **typescript** - TypeScript interfaces
4. **csharp** - C# classes
5. **python** - Python classes
6. **java** - Java classes
7. **go** - Go structs
8. **rust** - Rust structs
9. **graphql** - GraphQL schema
10. **react** - React TypeScript
11. **vue** - Vue Composition API
12. **swift** - Swift models
13. **dataverseSchema** - Dataverse schema
14. **powerAppsFormulas** - Power Apps formulas
15. **postgresqlDDL** - PostgreSQL DDL
16. **mysqlDDL** - MySQL DDL
17. **mongooseSchema** - Mongoose schema
18. **markdown** - Markdown documentation

### Example Request for All Formats
```json
{
  "entities": [
    {
      "name": "PRODUCT",
      "logicalName": "product",
      "attributes": [
        {"name": "id", "dataType": "int"},
        {"name": "name", "dataType": "string"},
        {"name": "price", "dataType": "decimal"},
        {"name": "category", "dataType": "string"}
      ]
    },
    {
      "name": "SUPPLIER", 
      "logicalName": "supplier",
      "attributes": [
        {"name": "id", "dataType": "int"},
        {"name": "company_name", "dataType": "string"},
        {"name": "contact_email", "dataType": "string"}
      ]
    }
  ],
  "relationships": [
    {
      "from": "PRODUCT",
      "to": "SUPPLIER",
      "type": "many-to-one"
    }
  ],
  "outputFormat": "all"
}
```

---

## 5. Error Testing Examples

### Invalid Diagram Syntax
```json
{
  "mermaidContent": "erDiagram\n    INVALID_ENTITY {\n        missing_type field_name\n        unclosed_bracket {\n    }"
}
```

### Unsupported Diagram Type
```json
{
  "mermaidContent": "unknownDiagram\n    A --> B"
}
```

### Empty Content
```json
{
  "mermaidContent": ""
}
```

---

## 6. Power Platform Testing Tips

### Using Power Automate
1. Create a new flow with "When an HTTP request is received" trigger
2. Add "Custom Connector" action
3. Select your Universal Mermaid Parser connector
4. Choose the operation (ParseMermaidDiagram, ConvertMermaidToFormats, or InvokeServer)
5. Provide the test JSON in the request body
6. Run the flow and examine the response

### Using Power Apps
1. Add the connector to your Power App
2. Create a button with an OnSelect action
3. Use the connector operations to process Mermaid diagrams
4. Display results in a gallery or text control

### Testing Checklist
- [ ] Test all three operations (ParseMermaidDiagram, ConvertMermaidToFormats, InvokeServer)
- [ ] Test each diagram type (ERD, Class, Flowchart, Sequence, etc.)
- [ ] Test all output formats
- [ ] Test MCP protocol with all 6 tools
- [ ] Test error handling with invalid inputs
- [ ] Test large diagram processing
- [ ] Verify response structure matches expected format

---

## 7. Performance Testing

### Large ERD Example
```
erDiagram
    USER {
        int user_id PK
        string username
        string email
        string password_hash
        datetime created_at
        datetime updated_at
        boolean is_active
    }
    PROFILE {
        int profile_id PK
        int user_id FK
        string first_name
        string last_name
        string bio
        string avatar_url
        date birth_date
    }
    POST {
        int post_id PK
        int user_id FK
        string title
        text content
        datetime created_at
        datetime updated_at
        boolean is_published
    }
    COMMENT {
        int comment_id PK
        int post_id FK
        int user_id FK
        text content
        datetime created_at
        boolean is_approved
    }
    LIKE {
        int like_id PK
        int post_id FK
        int user_id FK
        datetime created_at
    }
    TAG {
        int tag_id PK
        string tag_name
        string description
    }
    POST_TAG {
        int post_id FK
        int tag_id FK
    }
    
    USER ||--|| PROFILE : has
    USER ||--o{ POST : creates
    POST ||--o{ COMMENT : receives
    USER ||--o{ COMMENT : writes
    USER ||--o{ LIKE : gives
    POST ||--o{ LIKE : receives
    POST }o--o{ TAG : tagged_with
```

Use this comprehensive test suite to validate all functionality of your Universal Mermaid Connector!