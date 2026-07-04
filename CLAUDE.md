# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## **Memory Bank Usage (Essential)**  
This project uses a persistent memory bank system located in `memory-bank/`. At the start of EVERY task, you MUST read ALL memory bank files to understand project context, as this serves as the source of truth for project scope, architecture, and current state. The memory bank consists of:

- `projectbrief.md`: Core requirements and goals  
- `productContext.md`: Problems solved and user experience goals  
- `systemPatterns.md`: Architecture and design patterns  
- `techContext.md`: Technologies, setup, and constraints  
- `activeContext.md`: Current focus and next steps  
- `progress.md`: What works, what's left, and known issues  

After implementing significant changes, update the memory bank to reflect new insights, patterns, or project evolution.

## **Common Development Commands**  

### **Backend (API/)**  
- Build: `dotnet build API/ProjectX.sln`  
- Test: `dotnet test API/ProjectX.sln`  
- Run: `dotnet run --project API/src/API/API.csproj`  
- Swagger UI: Available at `https://localhost:5001/api` when `API:SwaggerEnabled=true`  

### **Unity Client (Client/)**  
- Primary development occurs through Unity Editor (version 6000.1.15f1)  
- Preserve `.meta` files when moving/adding assets  
- Avoid editing generated artifacts in `Client/Library/`, `Client/obj/`  
- Validation occurs via Unity Test Runner unless project-specific CLI tools are added  

## **Codebase Structure**  

### **Backend (API/)**  
Layered .NET solution:  
- `API/src/API`: ASP.NET Core entrypoint, endpoint mapping, OpenAPI/Swagger  
- `API/src/Application`: MediatR handlers, validators, DTOs, services  
- `API/src/Domain`: Entities, enums, constants (users, characters, inventory, quests, etc.)  
- `API/src/Infrastructure`: EF Core persistence, Identity, JWT, migrations  
- `API/tests/UnitTests`: xUnit test projects  

### **Unity Client (Client/)**  
- **Scenes**: Bootstrap, Main, Server, UI, Environment under `Client/Assets/Scenes/`  
- **Scripts**: Organized by concern in `Client/Assets/Scripts/`:  
  - `Network`: Player/enemy Netcode, health, crafting, transforms  
  - `UI`: Inventory, quests, crafting, character, cursor, translation UI  
  - `Shared`: Managers, helpers, web request, grid layout  
  - `Subscriptions`: Event handlers for gameplay state changes  
  - `Models`: DTOs mirroring backend contracts  
  - `Mono`: Bootstrap, spawner, audio, NPC behavior  
- **Assets**: TextMesh Pro resources (Futura PT font variants) in `Client/Assets/TextMesh Pro/Resources/`  

## **Key Technical Constraints & Patterns**  
- **Artifact Preservation**: Never modify `obj/`, `Library/`, `bin/` directories or generated files  
- **Localization**: i18n JSON files exist in both `Client/Assets/Resources/i18n/` and API contracts—validate when editing  
- **Cross-Platform Consistency**: Client DTOs closely mirror backend DTOs/commands  
- **Logging**: Serilog writes to console and daily rolling files  
- **Authentication**: ASP.NET Core Identity with JWT bearer tokens and role-based policies  
- **Generated Files**: `.csproj` files and Unity solution files (`Client.sln`, `ProjectXClient.sln`) are managed by respective IDEs  

## **Development Workflow**  
1. **Pre-Task**: Read all `memory-bank/` files to establish context  
2. **Implementation**: Follow existing patterns in code (naming, architecture, error handling)  
3. **Validation**:  
   - Backend: Run relevant unit tests  
   - Client: Verify in Unity Editor/Test Runner  
4. **Post-Task**: Update memory bank (`activeContext.md`, `progress.md`) with changes, insights, and next steps  
5. **Commits**: Preserve all `.meta` files; avoid committing generated/cache directories  

## **Project Context Summary**  
ProjectX is a networked RPG/survival-style prototype featuring:  
- Unity 6 client using Netcode for GameObjects and Unity Transport  
- ASP.NET Core backend with clean/domain-driven architecture  
- Synchronized game state (characters, inventory, quests, crafting, experience)  
- Multi-language support via i18n resources  
- Server-authoritative progression systems with client-side prediction  

> **Critical**: Always consult the memory bank before making architectural or gameplay decisions. When uncertain about product goals or implementation details, surface questions for clarification rather than assuming.  