#!/usr/bin/env bash
# =============================================================
# CountryAPI · Pruebas de todos los endpoints con curl
#
# Copia de la suite de TI.2.1 (WS_COUNTRYAPI_JAVA_RESTFul/pruebas/curl_countryapi.sh) con
# solo dos cambios: la URL base por defecto y la prueba 502, que en Java usaba asadmin y
# aquí usa el archivo appsettings.Pruebas.json del servidor (DUDAS.md, H10). Los casos,
# los códigos esperados y los datos son los mismos.
#
# Requisitos: servidor en marcha (dotnet run) y base ApiPaisesDB cargada
# (03. BDD/docker-compose.yml). En Windows se ejecuta desde Git Bash:
#
#   bash pruebas/curl_countryapi.sh
#
# Variables opcionales:
#   BASE       URL base de la API (por defecto http://localhost:5088/api)
#   SERVIDOR   carpeta desde la que corre el servidor, donde se escribe appsettings.Pruebas.json
#              para la prueba 502 (por defecto la carpeta del proyecto). Si no existe, se omite.
#
# Cada caso muestra el código HTTP recibido, el esperado y el cuerpo (recortado).
# Al final indica cuántos casos fallaron. El script crea y elimina el país de prueba
# «País de Prueba CURL», guarda Ecuador desde la API e importa países y universidades.
# =============================================================

BASE="${BASE:-http://localhost:5088/api}"
SERVIDOR="${SERVIDOR:-$(cd "$(dirname "$0")/.." && pwd)}"
JSON="Content-Type: application/json; charset=utf-8"

fallos=0
total=0
cuerpo=""

# llamar "título" "códigos esperados (ej. 200 o 200|502)" argumentos de curl...
llamar() {
    local titulo="$1" esperado="$2"
    shift 2
    local salida codigo estado
    salida=$(curl -s --max-time 60 -w $'\n%{http_code}' "$@")
    codigo="${salida##*$'\n'}"
    cuerpo="${salida%$'\n'*}"
    total=$((total + 1))
    if [[ "|$esperado|" == *"|$codigo|"* ]]; then
        estado="OK   "
    else
        estado="FALLA"
        fallos=$((fallos + 1))
    fi
    printf '\n[%s] %s\n       HTTP %s (esperado %s)\n       %.300s\n' "$estado" "$titulo" "$codigo" "$esperado" "$cuerpo"
}

echo "== CountryAPI · $BASE"

# ------------------------------------------------------------------ Login
echo; echo "---------------- Login"
llamar "Login con credenciales correctas" 200 \
    -X POST "$BASE/Login/login" -H "$JSON" -d '{"usuario":"monster","clave":"monster9"}'
TOKEN=$(printf '%s' "$cuerpo" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
if [[ -z "$TOKEN" ]]; then
    echo "No se obtuvo el token; revise que el servidor y la base estén activos."
    exit 1
fi
AUTH="Authorization: Bearer $TOKEN"

llamar "401 · Login con clave incorrecta" 401 \
    -X POST "$BASE/Login/login" -H "$JSON" -d '{"usuario":"monster","clave":"otra"}'
llamar "401 · Login con usuario inexistente" 401 \
    -X POST "$BASE/Login/login" -H "$JSON" -d '{"usuario":"nadie","clave":"monster9"}'
llamar "401 · Login sin datos" 401 \
    -X POST "$BASE/Login/login" -H "$JSON" -d '{}'

# ------------------------------------------------------------------ Token
echo; echo "---------------- Validar token"
llamar "401 · Listar países sin encabezado Authorization" 401 "$BASE/Paises"
llamar "401 · Listar países con un token inexistente" 401 \
    -H "Authorization: Bearer 00000000-0000-0000-0000-000000000000" "$BASE/Paises"
llamar "Token sin el prefijo Bearer (también se acepta)" 200 -H "Authorization: $TOKEN" "$BASE/Paises?nombre=ec"

# ------------------------------------------------------------------ CRUD
echo; echo "---------------- Gestionar países"
llamar "Listar países (ordenados por nombre)" 200 -H "$AUTH" "$BASE/Paises"
llamar "Buscar por nombre sin tildes ni mayúsculas (peru → Perú)" 200 -H "$AUTH" "$BASE/Paises?nombre=PERU"

llamar "201 · Registrar país" 201 -X POST "$BASE/Paises" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"País de Prueba CURL","capital":"Capital CURL","region":"América","subregion":"South America","poblacion":1000,"area":250.5,"moneda":"Dólar (USD)"}
EOF
ID=$(printf '%s' "$cuerpo" | sed -n 's/.*"datos":{"id":\([0-9]*\).*/\1/p')

llamar "409 · Registrar un país con nombre repetido" 409 -X POST "$BASE/Paises" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"País de Prueba CURL","region":"Europa","poblacion":1,"area":1}
EOF
llamar "400 · Registrar sin nombre" 400 -X POST "$BASE/Paises" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"","region":"América","poblacion":1,"area":1}
EOF
llamar "400 · Registrar sin región" 400 -X POST "$BASE/Paises" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"Otro país","poblacion":1,"area":1}
EOF
llamar "400 · Registrar con población negativa" 400 -X POST "$BASE/Paises" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"Otro país","region":"América","poblacion":-5,"area":1}
EOF
llamar "400 · Registrar con área negativa" 400 -X POST "$BASE/Paises" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"Otro país","region":"América","poblacion":5,"area":-1}
EOF
llamar "400 · Registrar con una región que no existe" 400 -X POST "$BASE/Paises" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"Otro país","region":"Atlántida","poblacion":5,"area":1}
EOF

llamar "Actualizar el país creado" 200 -X PUT "$BASE/Paises/$ID" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"País de Prueba CURL","capital":"Nueva capital","region":"Oceanía","subregion":"Polynesia","poblacion":2000,"area":300,"moneda":"Euro (EUR)"}
EOF
llamar "409 · Actualizar con el nombre de otro país (Ecuador)" 409 -X PUT "$BASE/Paises/$ID" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"Ecuador","region":"América","poblacion":1,"area":1}
EOF
llamar "404 · Actualizar un país inexistente" 404 -X PUT "$BASE/Paises/999999" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"No existe","region":"América","poblacion":1,"area":1}
EOF
llamar "Eliminar el país creado" 200 -X DELETE "$BASE/Paises/$ID" -H "$AUTH"
llamar "404 · Eliminar otra vez el mismo país" 404 -X DELETE "$BASE/Paises/$ID" -H "$AUTH"

# ------------------------------------------------------------------ Buscar y filtrar
echo; echo "---------------- Buscar y filtrar"
llamar "Buscar sin criterios (todos)" 200 -H "$AUTH" "$BASE/Paises/buscar"
llamar "Filtrar por región Europa" 200 -H "$AUTH" "$BASE/Paises/buscar?region=Europa"
llamar "Nombre parcial + región (a, América)" 200 -H "$AUTH" "$BASE/Paises/buscar?nombre=a&region=Am%C3%A9rica"
llamar "Capital parcial sin mayúsculas (quito)" 200 -H "$AUTH" "$BASE/Paises/buscar?capital=quito"
llamar "400 · Región no válida" 400 -H "$AUTH" "$BASE/Paises/buscar?region=Narnia"

# ------------------------------------------------------------------ Reportes
echo; echo "---------------- Reportes"
llamar "Generar los tres reportes" 200 -H "$AUTH" "$BASE/Reportes"

# ------------------------------------------------------------------ API REST Countries
echo; echo "---------------- Consultar país en la API (necesita internet y la clave de REST Countries v5)"
llamar "Consultar «ecuador» en la API" 200 -H "$AUTH" "$BASE/Paises/externo/ecuador"
llamar "404 · Nombre sin coincidencias en la API" 404 -H "$AUTH" "$BASE/Paises/externo/zzqqxxnoexiste"
llamar "400 · Nombre de un solo carácter" 400 -H "$AUTH" "$BASE/Paises/externo/e"
llamar "Guardar el país elegido (registra o actualiza)" 200 -X POST "$BASE/Paises/guardar" -H "$AUTH" -H "$JSON" --data-binary @- <<'EOF'
{"nombre":"Ecuador","capital":"Quito","region":"América","subregion":"South America","poblacion":17643060,"area":276841,"moneda":"United States dollar (USD)"}
EOF
llamar "Importar todos los países (3 páginas de la API, puede tardar)" 200 -X POST "$BASE/Paises/importar" -H "$AUTH"

# ------------------------------------------------------------------ Universidades
echo; echo "---------------- Universidades (necesita internet)"
llamar "Importar universidades de Ecuador" "200|502" -X POST "$BASE/Universidades/importar?pais=Ecuador" -H "$AUTH"
llamar "404 · País sin universidades en la API" "404|502" -X POST "$BASE/Universidades/importar?pais=Narnia" -H "$AUTH"
llamar "400 · Importar sin país" 400 -X POST "$BASE/Universidades/importar" -H "$AUTH"
llamar "Listar universidades guardadas de Ecuador" 200 -H "$AUTH" "$BASE/Universidades?pais=ecuador"

# ------------------------------------------------------------------ 502 simulado
echo; echo "---------------- 502 · API externa caída (simulada)"
if [[ -f "$SERVIDOR/appsettings.json" ]]; then
    # Apunta las APIs a un puerto sin servicio; la base no debe cambiar.
    # El servidor recarga appsettings.Pruebas.json sin reiniciar y lee las URL en cada petición.
    PRUEBAS="$SERVIDOR/appsettings.Pruebas.json"
    cat > "$PRUEBAS" <<'EOF'
{
  "CountryApi": {
    "RestCountriesUrl": "http://127.0.0.1:9/countries/v5",
    "UniversidadesUrl": "http://127.0.0.1:9"
  }
}
EOF
    sleep 2
    llamar "502 · Importar países con REST Countries caída" 502 -X POST "$BASE/Paises/importar" -H "$AUTH"
    llamar "502 · Consultar país con REST Countries caída" 502 -H "$AUTH" "$BASE/Paises/externo/ecuador"
    llamar "502 · Importar universidades con la API caída" 502 -X POST "$BASE/Universidades/importar?pais=Ecuador" -H "$AUTH"
    rm -f "$PRUEBAS"
    sleep 2
    llamar "Listar países sigue funcionando sin las APIs (RNF-008)" 200 -H "$AUTH" "$BASE/Paises?nombre=ecuador"
else
    echo "(omitida: no se encontró el servidor en $SERVIDOR)"
fi

# ------------------------------------------------------------------ Logout
echo; echo "---------------- Cerrar sesión"
llamar "Cerrar sesión" 200 -X POST "$BASE/Login/logout" -H "$AUTH"
llamar "401 · Usar el token después de cerrar sesión" 401 -H "$AUTH" "$BASE/Paises"

echo
echo "== $((total - fallos)) de $total casos con el código esperado; $fallos fallos."
exit "$fallos"
