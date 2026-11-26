# OpsBoard data access lab

Laboratorio incremental para observar el comportamiento de Entity Framework Core y estudiar cuándo una operación necesita otro nivel de control.

## Resultados de los laboratorios

Los tres primeros laboratorios se verificaron con 100 incidencias activas.

| Laboratorio | Estrategia | Comandos SQL | Cardinalidad estructural |
| --- | --- | ---: | ---: |
| [01 · Round trips ocultos](#laboratorio-01-round-trips-ocultos) | `per-incident` | 394 | — |
| 01 | `batched` | 5 | — |
| 01 | `projected` | 1 | — |
| [02 · Explosión cartesiana](#laboratorio-02-explosión-cartesiana-de-colecciones-hermanas) | `single-query` | 1 | 1.706 |
| 02 | `split-query` | 3 | 1.112 |

Cada laboratorio devuelve el mismo cuerpo HTTP para todas sus estrategias. Los
comandos se cuentan durante la ejecución. La cardinalidad estructural se calcula
a partir de las colecciones de la respuesta del Laboratorio 02.

El [Laboratorio 03](#laboratorio-03-la-entidad-completa-para-mostrar-ocho-campos)
leyó 100 filas en un comando con cada estrategia. La carga de entidades completas
seleccionó 27 columnas y creó 393 entidades de EF. La proyección seleccionó ocho
columnas y creó directamente los resúmenes, con cero entidades de EF.

El [Laboratorio 04](#laboratorio-04-el-endpoint-de-lectura-que-llenó-la-memoria)
recorrió 50.000 actualizaciones con un comando y el mismo JSON en cada estrategia.
Con `AsNoTracking()`, el pico de heap observado fue de 20,3 MiB al recorrer las
filas con `await foreach` y de 119,8 MiB al acumularlas con `ToListAsync()`.

## Requisitos

- .NET SDK 11 estable (11.0.100 o un parche posterior de esa banda)
- Un runtime de contenedores compatible con Aspire
- CLI de Aspire 17.0.0, fijada en el manifiesto de herramientas del repositorio

El AppHost y la orquestación usan Aspire 17. El laboratorio usa ASP.NET Core 11
y mantiene EF Core 10 con Npgsql 10 hasta que se publique el proveedor estable
para EF Core 11. La integración cliente `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`
se mantiene en `13.6.5`, su última versión estable publicada.

## Compilar

```bash
dotnet tool restore
dotnet restore
dotnet build --no-restore
```

Para trabajar con migraciones, ejecuta `dotnet tool restore` y los comandos
`dotnet ef` desde `src/OpsBoard.Data`, donde está fijada la versión de `dotnet-ef`.

## Ejecutar

```bash
dotnet tool run aspire -- run --apphost src/OpsBoard.AppHost/OpsBoard.AppHost.csproj
```

El AppHost levanta PostgreSQL 18, aplica las migraciones, prepara el dataset
`small` cuando el volumen está vacío y arranca la API. Los siguientes
arranques reutilizan el mismo volumen sin regenerar datos.

El dashboard de Aspire se abre sin autenticación y escucha en una dirección
local con puerto aleatorio. Esta configuración facilita el uso del laboratorio,
pero no debe copiarse a un entorno compartido: el dashboard expone trazas,
configuración y telemetría de la aplicación.

La tabla de recursos del dashboard muestra la URL asignada a `api`. Al abrirla,
la raíz redirige directamente a Scalar. La documentación interactiva también
está disponible explícitamente en `/scalar/v1`.

### Dataset `small`

La configuración reproducible se encuentra en
`src/OpsBoard.AppHost/appsettings.json`:

```text
Perfil:              small
Semilla:             1729
Instante de referencia: 2026-01-01T00:00:00Z
Versión del modelo:  1
```

El dataset contiene 500 incidencias, 4.000 actualizaciones, 1.100 asociaciones de
etiquetas, 100 periodos en pausa y 1.440 cambios de estado. PostgreSQL conserva
los datos en el volumen `opsboard-small-data`.

El generador se niega a modificar una base con metadata distinta o con datos
de origen desconocido. La creación inicial es determinista; al reutilizar el
volumen se validan la configuración y los conteos de incidencias, actualizaciones
y asociaciones de etiquetas. El fingerprint se calcula durante la generación.

Para volver al dataset inicial con Docker, detén primero el AppHost y elimina
exclusivamente el volumen del laboratorio:

```bash
docker volume rm opsboard-small-data
```

El siguiente arranque volverá a aplicar las migraciones y generar los datos.
La opción `--Dataset:Persist=false` arranca PostgreSQL con almacenamiento
temporal y permite repetir la prueba con datos nuevos, como hace CI.

El perfil `large` conserva 500 incidencias y genera 50.000 actualizaciones
deterministas para el Laboratorio 04. Usa el volumen `opsboard-large-data`
cuando se activa la persistencia.

## Laboratorio 01: round trips ocultos

La primera iteración compara tres formas de construir el mismo listado de
incidencias activas:

```http
GET /api/incidents/active?strategy=per-incident&limit=50
GET /api/incidents/active?strategy=batched&limit=50
GET /api/incidents/active?strategy=projected&limit=50
```

Los límites admitidos son `10`, `25`, `50` y `100`. Las tres estrategias devuelven
el mismo contrato y exponen información del experimento en las cabeceras:

```text
X-OpsBoard-Strategy
X-OpsBoard-Db-Queries
X-OpsBoard-Db-Query-Duration-Ms
X-OpsBoard-Data-Access-Elapsed-Ms
Server-Timing
```

Estas métricas forman parte del laboratorio y no representan un contrato de
producción. El parámetro `strategy` también existe únicamente para mantener
constante la operación HTTP mientras cambia la implementación de acceso a
datos.

Las estrategias distribuyen el trabajo de forma distinta:

- `per-incident`: materializa primero las incidencias y utiliza un cargador
  reutilizable para completar cada resultado.
- `batched`: carga los conjuntos necesarios mediante consultas explícitas y
  los relaciona en memoria.
- `projected`: mantiene la operación compuesta hasta proyectar el modelo de
  lectura.

Las tres comparten la definición del filtro, el orden y el límite. Las dos
estrategias que materializan primero la incidencia proyectan solo las columnas
que necesita el listado; de esta forma, la comparación no mezcla el número de
viajes con la carga accidental de columnas como `Description`.

Para `per-incident`, el número de consultas sigue la expresión:

```text
1 + 3N + A
```

`N` es el número de incidencias devueltas y `A` el número de ellas que tienen
usuario asignado. La consulta inicial aporta `1`; cada incidencia añade las
consultas de ownership, etiquetas y recuento de actualizaciones; las asignadas
añaden una consulta más.

El número de consultas es una propiedad estructural del experimento. Las
duraciones observadas sirven para explorar el escenario local, pero no son un
benchmark.

`X-OpsBoard-Db-Query-Duration-Ms` suma la duración comunicada por los eventos de
ejecución de comandos de EF. No representa el tiempo de CPU de PostgreSQL.
`X-OpsBoard-Data-Access-Elapsed-Ms` mide la estrategia completa hasta
materializar el resultado, pero no incluye la serialización HTTP.

Reducir consultas tampoco decide por sí solo qué implementación debe usarse:

- `per-incident` mantiene el enriquecimiento local y reutilizable, pero su coste
  crece con el número de resultados.
- `batched` acota los viajes y hace explícitos los conjuntos cargados, a cambio
  de diccionarios, memoria y listas de identificadores.
- `projected` minimiza viajes y columnas, pero delega al traductor la creación
  de una consulta SQL más compleja y dependiente del proveedor.

El listado inicial no incluye todavía un índice específico para el filtro por
estado y el orden por última actividad. Los tres caminos comparten esa
condición; el plan de acceso y el diseño de índices quedan fuera del alcance de
este laboratorio.

### Ejecutar el escenario comparativo

El arranque normal no ejecuta mediciones. Para añadir el `ScenarioRunner` al
AppHost:

```bash
dotnet tool run aspire -- run --apphost src/OpsBoard.AppHost/OpsBoard.AppHost.csproj -- --Scenarios:Run=true
```

El runner hace una petición de calentamiento y toma tres muestras por estrategia
y límite. Para cada muestra exige el número de incidencias solicitado en el
dataset `small`, el número esperado de comandos SQL y el mismo SHA-256 del cuerpo
que la primera muestra del límite. Un fallo identifica la estrategia, el límite
y la muestra en los logs del recurso `scenario-runner`.

`dotnet test` valida el contrato de entrada, la instrumentación, la forma del
modelo, la generación determinista y el SQL de la consulta base. El
`ScenarioRunner` valida los laboratorios por HTTP contra PostgreSQL. GitHub
Actions ejecuta las pruebas y arranca Aspire con una base nueva para exigir que
el runner termine correctamente.

### Seguir una petición en Aspire

El cliente oficial de Aspire para Npgsql/EF Core añade las consultas a la traza
de la petición. En el dashboard:

1. Abre `Seguimientos`.
2. Selecciona una petición `GET /api/incidents/active`.
3. Expande la petición para ver sus consultas a `opsboard`.
4. Selecciona una consulta para inspeccionar `db.query.text`.

Las consultas incluyen comentarios `TagWith` como
`Incident list batch: active incidents`, lo que permite relacionar cada SQL con
la parte correspondiente del código. En una ejecución del escenario, el árbol
de la traza hace visible la diferencia entre 394, 5 y 1 consultas sin depender
exclusivamente de las cabeceras HTTP.

## Laboratorio 02: explosión cartesiana de colecciones hermanas

Este laboratorio carga el mismo detalle de incidencias activas con dos
colecciones hermanas: `tags` y `updates`. Cuando ambas colecciones se incluyen
en una sola consulta SQL, las filas de una se combinan con las de la otra para
cada incidencia. El resultado es una multiplicación de filas, aunque EF Core
reconstruya correctamente las colecciones al materializar el resultado.

Las únicas estrategias del laboratorio son:

- `single-query`: aplica `AsSingleQuery()` y ejecuta un comando SQL con el
  producto de las colecciones hermanas.
- `split-query`: aplica `AsSplitQuery()` sobre la misma consulta lógica y
  ejecuta un comando para la raíz y otro para cada colección.

Ambas estrategias comparten filtro, orden, límite, `Include`, DTO y mapper. El
endpoint es:

```http
GET /api/incidents/active/details?strategy=single-query&limit=50
GET /api/incidents/active/details?strategy=split-query&limit=50
```

Los límites admitidos son `10`, `25`, `50` y `100`. El detalle expone las
cabeceras de consultas y duración ya usadas por el Lab 01. Se investigó
`DataReaderDisposingEventData.ReadCount` y los eventos de cierre durante el
desarrollo del laboratorio con EF Core 11 Preview 6,
pero en `AsSplitQuery()` la instrumentación no ofrece de forma fiable el total
de filas de los tres lectores. Por eso el laboratorio no publica una cabecera
de filas leídas ni presenta una estimación como medición.

El `ScenarioRunner` prueba ambos caminos con límites de `10`, `50` y `100`.
Además de las comprobaciones por muestra, publica la cardinalidad estructural
esperada a partir de la respuesta:

```text
single-query = suma(max(1, tags) * max(1, updates))
split-query  = incidentes + tags + updates
```

Con la configuración de referencia de `small`, cada muestra debe reproducir
las cardinalidades de referencia para su límite y estrategia.

Se puede ejecutar con:

```bash
dotnet tool run aspire -- run --apphost src/OpsBoard.AppHost/OpsBoard.AppHost.csproj -- --Scenarios:Run=true
```

El informe JSON aparece en la salida del recurso `scenario-runner` del
dashboard de Aspire. Las trazas de Aspire/Npgsql permiten inspeccionar el SQL
con los comentarios `TagWith` y relacionarlo con cada petición.

`AsSplitQuery()` no es universalmente mejor: evita el producto cartesiano de
colecciones hermanas, pero añade comandos y round trips y puede tener
consideraciones de consistencia si los datos cambian entre consultas. Los
resultados dependen del proveedor, la versión de EF Core, la cardinalidad y la
latencia. Este laboratorio es una demostración reproducible, no un benchmark
definitivo; sus mediciones corresponden a este dataset y entorno.

## Laboratorio 03: la entidad completa para mostrar ocho campos

El resumen de incidencias activas devuelve `number`, `title`, `status`,
`severity`, `service`, `team`, `assignee` y `lastActivityAt`.
Ambos caminos comparten filtro, orden, límite y el mismo mapeo al DTO. El
responsable es opcional.

```http
GET /api/incidents/active/summary?strategy=full-entity&limit=50
GET /api/incidents/active/summary?strategy=projected&limit=50
```

`full-entity` carga la incidencia, su servicio, su equipo y su responsable
mediante `Include` y `AsNoTracking()`. Construye el DTO en memoria después de
materializar las entidades. `projected`, la estrategia predeterminada, aplica
el mismo mapeo con `Select` antes de ejecutar el SQL. Las dos consultas cargan
referencias y devuelven una fila por incidencia.

Además de las cabeceras comunes, el endpoint expone las filas del lector
(`X-OpsBoard-Db-Rows`), sus columnas (`X-OpsBoard-Db-Columns`) y las instancias
de entidades creadas por EF (`X-OpsBoard-Materialized-Entities`). El recuento
de filas se toma al liberar el lector del Lab 03. EF Core 10 también cuenta la
llamada final a `Read` que devuelve `false`, de modo que se descuenta esa
llamada tras consumir el resultado completo.

Con `--Scenarios:Run=true`, el runner comprueba un comando, una fila por
incidencia, ocho campos JSON y el mismo SHA-256 para ambas estrategias en los
límites `10`, `25`, `50` y `100`. La proyección debe recuperar ocho columnas y
materializar cero entidades de EF. Los comentarios `TagWith` permiten comparar
el SQL en las trazas de Aspire.

## Laboratorio 04: el endpoint de lectura que llenó la memoria

El endpoint `GET /api/incident-updates/scan` recorre actualizaciones por `Id` e
incluye el autor de cada una. Acepta `strategy` y un `limit` entre `1` y `50000`.
Por defecto usa `no-tracking` con un límite de `1000`. Cada estrategia devuelve
un recuento, el número de autores distintos y una huella SHA-256 de sus
identificadores, el tipo de actualización y su fecha en UTC.

| Estrategia | Modo de EF Core | Consumo de los resultados |
| --- | --- | --- |
| `tracking` | `AsTracking()` | `await foreach` |
| `no-tracking` | `AsNoTracking()` | `await foreach` |
| `identity-resolution` | `AsNoTrackingWithIdentityResolution()` | `await foreach` |
| `buffered` | `AsNoTracking()` | `ToListAsync()` |

Las tres primeras variantes aíslan el tracking y la resolución de identidad.
`no-tracking` y `buffered` aíslan el efecto de retener la lista. Todas ejecutan el
mismo SQL y usan un contexto nuevo sin reintentos, ya que los reintentos de EF
Core pueden acumular internamente las filas de una consulta.

El runner valida un comando, una fila por actualización y un cuerpo idéntico
por límite. Si se leen `N` actualizaciones con `A` autores distintos,
`tracking` e `identity-resolution` materializan `N + A` entidades. Las otras
dos estrategias materializan `2N`. Solo `tracking` conserva entradas en el
`ChangeTracker` del contexto al terminar el recorrido. `identity-resolution`
mantiene su propio registro de entidades durante la enumeración para reutilizar
la instancia de cada autor.

### Medir con el perfil large

Desde la raíz del repositorio, compila en Release y ejecuta el mismo script
que usa CI una vez por estrategia. Cada invocación arranca un proceso de API
nuevo y ejecuta el Lab 04 con 1.000, 10.000 y 50.000 filas. La ejecución normal
de CI usa `small` y comprueba las invariantes con 1.000 y 4.000 filas.
El directorio temporal evita que Aspire reutilice la ruta a un AppHost de
Debug guardada en la caché de una ejecución anterior.

```bash
dotnet build -c Release
(
  set -e
  export ASPIRE_HOME="$(mktemp -d)"
  export Configuration=Release
  for strategy in tracking no-tracking identity-resolution buffered; do
    bash .github/scripts/verify-scenarios.sh --Dataset:Profile=large \
      "--Scenarios:Lab04Strategy=$strategy"
  done
)
```

El informe recoge la mediana de tres muestras después de una petición de
calentamiento por límite. Los bytes asignados y el heap administrado son
observaciones del proceso de API. El heap se muestrea cada 10 ms, al comenzar
y terminar la operación, y justo después de llenar la lista en `buffered`.
La duración de acceso incluye el recorrido y el cálculo de la huella. Las
mediciones dependen del entorno y de las colecciones del GC.

### Resultados con 50.000 actualizaciones

Medición local en Release, en macOS arm64 con .NET SDK 11.0.100, Aspire 17 y
PostgreSQL 18. Para cada límite, las cuatro estrategias devolvieron el mismo
JSON con un comando y 10 columnas. A 50.000 filas, las cuatro recorrieron 145
autores distintos.

| Estrategia | Entidades materializadas | Entradas en el contexto al terminar | Bytes asignados (MiB) | Heap inicial (MiB) | Pico de heap observado (MiB) | Acceso y recorrido (ms) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `tracking` | 50.145 | 50.145 | 171,0 | 89,6 | 117,8 | 366,49 |
| `no-tracking` | 100.000 | 0 | 221,0 | 17,4 | 20,3 | 272,99 |
| `identity-resolution` | 50.145 | 0 | 187,4 | 76,4 | 134,7 | 284,17 |
| `buffered` | 100.000 | 0 | 222,0 | 77,5 | 119,8 | 318,72 |

`no-tracking` y `buffered` asignaron cantidades parecidas y materializaron el
mismo número de entidades. Al recorrer las filas sin acumularlas en una lista,
el GC pudo recuperar objetos durante la lectura. La resolución de identidad
evitó materializar 49.855 instancias de autor repetidas y mantuvo su registro
temporal durante el recorrido.

## Formato JSON del runner

En la ejecución normal, la raíz contiene `capturedAt` (inicio de la ejecución
en UTC) y los objetos `lab01`, `lab02`, `lab03` y `lab04`. Con
`Scenarios:Lab04Strategy`, la raíz contiene solo `lab04`. Cada objeto de
laboratorio contiene `scenario` (nombre del escenario), `capturedAt` (fin del
escenario en UTC) y `results` (una entrada por estrategia y límite). Los
consumidores del informe original del Laboratorio 01 deben cambiar `.results`
por `.lab01.results`.

| Campo de `results` | Significado |
| --- | --- |
| `strategy`, `limit` | Estrategia y límite solicitados. |
| `itemCount`, `queryCount` | Número de elementos devueltos o recorridos y comandos SQL validados en cada muestra. |
| `medianQueryExecutionMilliseconds`, `medianDataAccessMilliseconds` | Medianas de las tres muestras para eventos de ejecución de comandos y acceso a datos hasta materializar el resultado. |
| `bodySha256` | SHA-256 del cuerpo HTTP, común a todas las muestras y estrategias del mismo límite. |
| `expectedStructuralRows` | Cardinalidad estructural calculada a partir del cuerpo. Solo aparece en `lab02.results`. |
| `rowCount`, `columnCount`, `materializedEntityCount` | Filas y columnas del lector e instancias de entidades creadas por EF. Aparecen en `lab03.results` y `lab04.results`. |
| `distinctAuthorCount`, `trackedEntries` | Autores distintos y entradas retenidas en el contexto al terminar el Lab 04. |
| `memory` | `medianAllocatedBytes`, `medianBaselineManagedBytes` y `medianPeakManagedBytes`. Solo aparece en el Lab 04 con `Scenarios:Lab04Strategy`. |

Para extraer la comparación del Laboratorio 02 de un informe guardado:

```bash
jq '.lab02.results[] | {strategy, limit, queryCount, expectedStructuralRows}' informe.json
```

### Métricas de lectura del Laboratorio 03

```bash
jq '.lab03.results[] | {strategy, limit, queryCount, rowCount, columnCount, materializedEntityCount}' informe.json
```

### Métricas de lectura del Laboratorio 04

```bash
jq '.lab04.results[] | {strategy, limit, materializedEntityCount, trackedEntries, memory}' informe.json
```
