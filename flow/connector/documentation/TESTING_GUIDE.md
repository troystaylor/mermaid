# Testing Guide: Mermaid to Dataverse Custom Connector

## 🎯 **Testing Overview**

This guide validates the complete functionality of the Mermaid to Dataverse Custom Connector with actual Dataverse API operations using `Context.SendAsync`.

## 📋 **Pre-Test Setup**

### 1. Environment Requirements
- **Power Platform Environment**: Admin access required
- **Dataverse Database**: Must be provisioned
- **Azure AD App**: OAuth 2.0 application registration
- **Connector Deployment**: Must be successfully deployed

### 2. Connector Information
- **Connector Name**: Mermaid to Dataverse Converter
- **Version**: 1.0.0 (with Context.SendAsync implementation)
- **Operations**: 2 (ParseMermaidERD, ConvertAndUpsertToDataverse)
- **Authentication**: Multi-auth (No Auth + OAuth 2.0)

## 🧪 **Test Cases**

### Test 1: Parse Mermaid ERD (No Authentication)
**Purpose**: Validate universal Mermaid parsing without Dataverse operations

**Payload**:
```json
{
  "mermaidDiagram": "erDiagram\n    CONTACT {\n        string firstname\n        string lastname\n        string email\n    }\n    ACCOUNT {\n        string name\n        string industry\n    }\n    CONTACT ||--o{ ACCOUNT : \"works for\"\n"
}
```

**Expected Response**:
```json
{
  "success": true,
  "diagramType": "erDiagram",
  "tablesDetected": 2,
  "tables": ["CONTACT", "ACCOUNT"],
  "relationships": 1,
  "cdmEntitiesDetected": ["Contact", "Account"]
}
```

**Validation Points**:
- ✅ Response returns within 5 seconds
- ✅ All tables detected correctly
- ✅ CDM entities identified
- ✅ Relationship count accurate

### Test 2: Simple Table Creation (OAuth Required)
**Purpose**: Create new Dataverse tables with basic attributes

**Prerequisites**:
- OAuth 2.0 connection established
- Environment URL configured
- Admin permissions in target environment

**Payload**:
```json
{
  "mermaidDiagram": "erDiagram\n    PRODUCT {\n        string name\n        decimal price\n        integer quantity\n        boolean active\n    }",
  "dataverseEnvironmentUrl": "https://[your-environment].crm.dynamics.com",
  "createNewTables": true,
  "updateExistingTables": false,
  "createRelationships": false
}
```

**Expected Response**:
```json
{
  "success": true,
  "operationsExecuted": 1,
  "tablesCreated": 1,
  "tablesUpdated": 0,
  "relationshipsCreated": 0,
  "results": [
    {
      "operation": "CreateTable",
      "tableName": "new_product",
      "success": true,
      "attributesCreated": 4
    }
  ]
}
```

**Manual Verification**:
1. Navigate to Power Apps > Tables
2. Verify `new_product` table exists
3. Check attributes: `new_name`, `new_price`, `new_quantity`, `new_active`
4. Validate attribute types match Mermaid specification

### Test 3: Complex Table with Relationships (OAuth Required)
**Purpose**: Create multiple tables with one-to-many relationships

**Payload**:
```json
{
  "mermaidDiagram": "erDiagram\n    CUSTOMER {\n        string firstname\n        string lastname\n        string email\n        datetime createdon\n    }\n    ORDER {\n        string ordernumber\n        decimal totalamount\n        datetime orderdate\n    }\n    CUSTOMER ||--o{ ORDER : \"places\"\n",
  "dataverseEnvironmentUrl": "https://[your-environment].crm.dynamics.com",
  "createNewTables": true,
  "updateExistingTables": true,
  "createRelationships": true
}
```

**Expected Response**:
```json
{
  "success": true,
  "operationsExecuted": 3,
  "tablesCreated": 2,
  "tablesUpdated": 0,
  "relationshipsCreated": 1,
  "results": [
    {
      "operation": "CreateTable",
      "tableName": "new_customer",
      "success": true
    },
    {
      "operation": "CreateTable", 
      "tableName": "new_order",
      "success": true
    },
    {
      "operation": "CreateRelationship",
      "success": true,
      "relationshipName": "new_customer_new_order"
    }
  ]
}
```

**Manual Verification**:
1. Check both tables created: `new_customer`, `new_order`
2. Verify relationship exists: Customer → Order (one-to-many)
3. Check lookup field created: `new_customerid` in Order table
4. Validate relationship metadata and cascade behavior

### Test 4: Update Existing Table (OAuth Required)
**Purpose**: Add new attributes to existing table

**Prerequisites**: 
- `new_customer` table exists from previous test

**Payload**:
```json
{
  "mermaidDiagram": "erDiagram\n    CUSTOMER {\n        string firstname\n        string lastname\n        string email\n        string phonenumber\n        string address\n        datetime createdon\n    }",
  "dataverseEnvironmentUrl": "https://[your-environment].crm.dynamics.com",
  "createNewTables": false,
  "updateExistingTables": true,
  "createRelationships": false
}
```

**Expected Response**:
```json
{
  "success": true,
  "operationsExecuted": 1,
  "tablesCreated": 0,
  "tablesUpdated": 1,
  "relationshipsCreated": 0,
  "results": [
    {
      "operation": "UpdateTable",
      "tableName": "new_customer",
      "success": true,
      "newAttributesAdded": 2,
      "existingAttributesSkipped": 4
    }
  ]
}
```

**Manual Verification**:
1. Check `new_customer` table
2. Verify new attributes: `new_phonenumber`, `new_address`
3. Confirm existing attributes unchanged
4. Validate no duplicates created

### Test 5: Universal Mermaid Support (No Authentication)
**Purpose**: Validate support for non-ERD diagram types

**Payload**:
```json
{
  "mermaidDiagram": "flowchart TD\n    A[Start] --> B{Decision}\n    B -->|Yes| C[Action 1]\n    B -->|No| D[Action 2]\n    C --> E[End]\n    D --> E"
}
```

**Expected Response**:
```json
{
  "success": true,
  "diagramType": "flowchart",
  "message": "Flowchart diagram detected. This diagram type focuses on process flow rather than data entities. No Dataverse tables can be extracted from this diagram type.",
  "extractedElements": {
    "nodes": ["Start", "Decision", "Action 1", "Action 2", "End"],
    "nodeCount": 5
  }
}
```

**Validation Points**:
- ✅ Non-ERD diagrams handled gracefully
- ✅ Appropriate messaging for unsupported types
- ✅ No errors or crashes
- ✅ Parsing still provides useful information

### Test 6: Error Handling (OAuth Required)
**Purpose**: Validate error handling for invalid scenarios

**Test 6A: Invalid Environment URL**
```json
{
  "mermaidDiagram": "erDiagram\n    TEST { string name }",
  "dataverseEnvironmentUrl": "https://invalid-environment.crm.dynamics.com",
  "createNewTables": true
}
```

**Expected Response**:
```json
{
  "success": false,
  "error": "Failed to connect to Dataverse environment",
  "details": "The specified environment URL is invalid or inaccessible"
}
```

**Test 6B: Invalid Mermaid Syntax**
```json
{
  "mermaidDiagram": "invalid mermaid syntax here",
  "dataverseEnvironmentUrl": "https://[valid-environment].crm.dynamics.com"
}
```

**Expected Response**:
```json
{
  "success": false,
  "error": "Failed to parse Mermaid diagram",
  "details": "Invalid Mermaid syntax detected"
}
```

## 📊 **Test Results Template**

### Test Execution Log
```
Test Date: [Date]
Environment: [Environment URL]
Connector Version: 1.0.0
Tester: [Name]

Test 1 - Parse ERD: [PASS/FAIL]
Test 2 - Simple Creation: [PASS/FAIL]  
Test 3 - Complex with Relations: [PASS/FAIL]
Test 4 - Update Existing: [PASS/FAIL]
Test 5 - Universal Support: [PASS/FAIL]
Test 6 - Error Handling: [PASS/FAIL]

Overall Result: [PASS/FAIL]
Issues Found: [List any issues]
```

## 🔧 **Troubleshooting Guide**

### Common Issues

**Issue**: "Authorization failed"
- **Cause**: OAuth token expired or invalid
- **Solution**: Recreate OAuth connection in Power Platform

**Issue**: "Table already exists" error
- **Cause**: Table exists but updateExistingTables = false
- **Solution**: Set updateExistingTables = true or use unique table names

**Issue**: "Insufficient permissions"
- **Cause**: User lacks admin rights in target environment
- **Solution**: Grant System Administrator role or use admin account

**Issue**: "Environment not found"
- **Cause**: Invalid environment URL format
- **Solution**: Use format: https://[orgname].crm[X].dynamics.com

### Performance Expectations

- **Parse Operation**: < 5 seconds
- **Single Table Creation**: < 30 seconds
- **Complex with Relationships**: < 60 seconds
- **Large Diagrams (10+ tables)**: < 2 minutes

## ✅ **Success Criteria**

The connector passes testing if:
1. All test cases execute without errors
2. Tables are created in Dataverse as expected
3. Attributes match Mermaid specifications exactly
4. Relationships are properly established
5. Error handling works correctly
6. Performance meets expectations
7. Multi-auth functionality works

## 📝 **Test Report Template**

After completing all tests, document:
- ✅ Tests passed vs. total tests
- 🏆 Key successes and capabilities validated
- ⚠️ Issues or limitations discovered
- 📈 Performance metrics observed
- 💡 Recommendations for improvements
- 🎯 Readiness for production use

This comprehensive testing validates our complete Mermaid to Dataverse conversion capability with actual API operations!