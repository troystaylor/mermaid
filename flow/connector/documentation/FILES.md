# Mermaid ERD Processor - Connector Files

## 📁 Production Files

### Required Connector Files
- **`apiDefinition.swagger.json`** - Swagger 2.0 API definition for Power Platform
- **`apiProperties.json`** - Connection parameter sets with anonymous and OAuth authentication
- **`script.csx`** - Production C# custom code (1,500+ lines)
- **`settings.json`** - Test parameters and configuration

### Documentation Files
- **`readme.md`** - Complete connector documentation for Independent Publisher submission
- **`SUBMISSION_PACKAGE.md`** - Submission checklist and guidelines

## 📁 Archive Files
Located in `archive/` subfolder:
- **`script-compatible.csx`** - Development version (identical to script.csx)
- **`script-minimal.csx`** - Original minimal working version (150 lines)

## ✅ Deployment Status

**Successfully Deployed**
- **Connector ID**: `480bea10-298f-f011-b4cb-000d3a34f68f`
- **Environment**: Bosmang  
- **Status**: Active and operational
- **Version**: Production-ready enhanced implementation

## 🎯 Key Features

- **Universal Mermaid Support**: All 20+ diagram types (ERD, Class, Flowchart, Sequence, etc.)
- **Complete Dataverse Integration**: All 26 official column types supported
- **CDM Entity Detection**: 50+ Common Data Model entity mappings
- **Flexible Authentication**: Connection parameter sets for anonymous parsing or user-provided OAuth credentials
- **User-Controlled Security**: Users provide their own Azure AD app registration details
- **Production-Grade Code**: Comprehensive error handling, validation, and logging
- **Independent Publisher Ready**: Meets all Microsoft certification requirements

## 🚀 Usage

The connector provides two main operations with flexible authentication:

### Authentication Options
- **No Authentication**: For parsing Mermaid diagrams only (read-only operations)
- **Power Platform OAuth**: For full functionality including Dataverse operations (requires Azure AD app registration)

### Operations

1. **Parse Mermaid** (`/parse-mermaid`) - *No authentication required*
   - Parse and validate any Mermaid diagram
   - Extract structured data and relationships
   - CDM entity detection and validation

2. **Convert and Upsert** (`/convert-and-upsert`) - *OAuth authentication required*
   - Convert ERD/Class diagrams to Dataverse operations
   - Generate table creation/update operations
   - Handle relationships and data types
   - **Requires Azure AD app registration** with Power Platform API permissions

## 📋 File Management

### Production Deployment
Use these files for production deployment and Independent Publisher submission:
- `apiDefinition.swagger.json`
- `apiProperties.json` 
- `script.csx`
- `settings.json`
- `readme.md`

### Development Archive
Located in `archive/` subfolder - keep for reference but not needed for deployment:
- `script-compatible.csx` (identical to script.csx)
- `script-minimal.csx` (original simple version)

The connector is **production-ready** and successfully deployed! 🎉