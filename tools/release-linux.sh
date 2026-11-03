#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="$(grep -oP '(?<=<Version>)[^<]+' "$ROOT/Directory.Build.props" | head -n1)"
RID="linux-x64"
OUT="$ROOT/dist/linux"
APP="$OUT/app"
PLAYER="$OUT/player"
PACKAGE="Ollin-${VERSION}-linux-x64"

mkdir -p "$APP" "$PLAYER"

echo "Publicando Ollin $VERSION para $RID..."
# IncludeAllContentForSelfExtract deja los ensamblados como archivos reales en disco:
# Roslyn (scripts del usuario) resuelve sus referencias con TRUSTED_PLATFORM_ASSEMBLIES
# y en un single-file puro esas rutas no existen, así que los scripts no compilarían.
dotnet publish "$ROOT/src/Ollin.Editor/Ollin.Editor.csproj" \
  -c Release -r "$RID" --self-contained true \
  -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:DebugType=none -o "$APP"

dotnet publish "$ROOT/src/Ollin.Player/Ollin.Player.csproj" \
  -c Release -r "$RID" --self-contained true \
  -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:DebugType=none -o "$PLAYER"

chmod +x "$APP/Ollin.Editor" "$PLAYER/Ollin.Player"

# Las plantillas de proyecto buscan las texturas junto al ejecutable
# (AppContext.BaseDirectory/assets); sin esto, los proyectos nuevos salen sin sprites.
# La copia con "/." es idempotente: repetir el empaquetado no anida assets/assets.
mkdir -p "$APP/assets"
cp -a "$ROOT/assets/." "$APP/assets/"

WORK="$OUT/package/$PACKAGE"
rm -rf "$OUT/package"
mkdir -p "$WORK"
cp -a "$APP"/. "$WORK/app/"
cp -a "$PLAYER"/. "$WORK/player/"
cp "$ROOT/docs/LINUX.md" "$WORK/LEEME-LINUX.md"

cat > "$WORK/ollin.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Ollin
Comment=Motor de juego 2D
Exec=@APP@/Ollin.Editor
Icon=applications-games
Terminal=false
Categories=Development;Game;
EOF

cat > "$WORK/install.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEST="${XDG_DATA_HOME:-$HOME/.local/share}/Ollin"
BIN="${HOME}/.local/bin"
mkdir -p "$DEST" "$BIN" "${XDG_DATA_HOME:-$HOME/.local/share}/applications"
cp -a "$HERE/app"/. "$DEST/app/"
cp -a "$HERE/player"/. "$DEST/player/"
chmod +x "$DEST/app/Ollin.Editor" "$DEST/player/Ollin.Player"
ln -sf "$DEST/app/Ollin.Editor" "$BIN/ollin"
sed "s|@APP@|$DEST/app|g" "$HERE/ollin.desktop" > "${XDG_DATA_HOME:-$HOME/.local/share}/applications/ollin.desktop"
echo "Ollin instalado en $DEST"
echo "Ejecuta: ollin  (si ~/.local/bin está en PATH)"
EOF
chmod +x "$WORK/install.sh"

tar -C "$OUT/package" -czf "$OUT/$PACKAGE.tar.gz" "$PACKAGE"
echo "Paquete: $OUT/$PACKAGE.tar.gz"
