# CLAUDE.md

## Propósito del repositorio

Este repo es un **laboratorio de aprendizaje de IA aplicada a código legacy**. Contiene un sistema de facturación **ficticio** (`SistFact`) escrito a propósito al estilo de un proyecto .NET Framework 4 de 2010: métodos largos, lógica de negocio mezclada con acceso a datos, nombres crípticos, sin comentarios y sin tests.

El objetivo no es mantener un producto real, sino **practicar cómo usar asistentes de IA para entender, documentar, testear y refactorizar código heredado** de forma segura e incremental.

> El código "feo" es intencional. No es un descuido que haya que corregir de oficio.

## Estructura

```
SistFact.sln                      Solución formato VS2010
src/SistFact.Negocio/             Biblioteca de clases (.NET Framework 4.0)
  FactMgr.cs                      La clase legacy: ítems, impuestos, descuentos, validaciones, persistencia
src/SistFact.Consola/             App de consola que ejercita FactMgr con varios escenarios
  Program.cs
```

- Los proyectos son `.csproj` **de estilo antiguo** (no SDK-style), con `TargetFrameworkVersion v4.0`.
- `FactMgr` tiene un **modo test** (`FactMgr.modoTest = true`) que usa datos en memoria (productos y clientes de ejemplo). La consola lo activa por defecto.
- El modo producción (`SistFact.Consola.exe /prod`) apunta a un SQL Server que **no existe**. No intentes conectarte ni levantar una base salvo que el ejercicio lo pida.

## Compilar y ejecutar

```powershell
dotnet build SistFact.sln
.\src\SistFact.Consola\bin\Debug\SistFact.Consola.exe
```

Requiere el SDK de .NET y los reference assemblies de .NET Framework 4.0 (o, como alternativa, MSBuild de Visual Studio).

## Reglas de trabajo para Claude

1. **No modernizar ni "arreglar" el código por iniciativa propia.** Solo cambiar lo que pida el ejercicio en curso. Si ves problemas fuera del alcance, mencionalos sin tocarlos.
2. **Preservar el comportamiento observable** al refactorizar, salvo que el ejercicio sea justamente corregir un bug. Ante la duda, primero escribir tests de caracterización que fijen el comportamiento actual.
3. **Cambios pequeños y verificables.** Un paso de refactor por vez, compilando y corriendo la consola o los tests después de cada uno.
4. **Explicar el razonamiento.** Esto es un entorno de aprendizaje: al proponer un cambio, decir qué olor de código se ataca, qué técnica se aplica y qué riesgo tiene.
5. **Respetar el target .NET Framework 4.0** mientras no se decida explícitamente migrar. Ojo con features de C# o APIs que no existen en ese framework.
6. Al pedir análisis (bugs, riesgos, deuda técnica), **descubrirlos leyendo el código**; este archivo deliberadamente no los enumera.
7. Responder en español.

## Ejercicios sugeridos

- Explicar qué hace `FactMgr.Proc` paso a paso y dibujar su flujo.
- Inferir y documentar las reglas de negocio (impuestos, descuentos, validaciones) a partir del código.
- Agregar un proyecto de tests y escribir tests de caracterización sobre el modo test.
- Detectar bugs, problemas de seguridad y de concurrencia, y priorizarlos.
- Renombrar y extraer métodos para separar validación, cálculo y persistencia.
- Introducir una abstracción de acceso a datos para poder testear sin `modoTest`.
- Reemplazar `ArrayList`/`object[]` por tipos con nombre.
- Planificar una migración a .NET moderno (SDK-style, `decimal`, ADO.NET parametrizado o un ORM).

## Convenciones de git

- `main` conserva la versión legacy original como punto de partida.
- Cada ejercicio se hace en su propia rama (por ejemplo `ejercicio/01-tests-caracterizacion`).
