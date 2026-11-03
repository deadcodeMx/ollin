# Ollin en Linux

## Requisitos

- .NET 8 runtime (o usa el paquete self-contained).
- OpenGL ES / Mesa.
- Fuentes básicas (`fontconfig`).
- Para audio: por ahora el backend Linux es silencioso; el juego arranca igual.

En Debian/Ubuntu:

```bash
sudo apt install libgl1-mesa-dri libgles2 libegl1 libfontconfig1
```

## Ejecutar el editor

```bash
tar -xzf Ollin-*-linux-x64.tar.gz
cd Ollin-*-linux-x64
chmod +x app/Ollin.Editor player/Ollin.Player
./app/Ollin.Editor
```

O instalar en el perfil:

```bash
bash install.sh
ollin
```

## Jugar un proyecto

```bash
./app/Ollin.Editor --project /ruta/al/proyecto
```

El reproductor exportado usa `./player/Ollin.Player`.

## Limitaciones de la primera versión Linux

- Audio silencioso: falta integrar un backend ALSA/PulseAudio/OpenAL.
- Gamepad: lector de `/dev/input/js*`; puede requerir permisos del grupo `input`.
- El instalador gráfico es solo para Windows; en Linux se usa `install.sh`.
- La exportación funciona, pero el binario Linux debe generarse en Linux.
