# Releasing SnusStop to Google Play

This app targets **`net10.0-android`** only. Everything below runs from the project root
(`Nicotine_Stop.csproj`).

## 1. One-time: create an upload keystore

You (the developer) hold the signing key — never commit it.

```bash
keytool -genkeypair -v -keystore snusstop-upload.keystore \
  -alias snusstop -keyalg RSA -keysize 2048 -validity 10000
```

Store the keystore file and the passwords somewhere safe (a password manager). If you lose the
upload key you can reset it via Play App Signing, but keep it anyway.

## 2. Version bumps (every release)

In `Nicotine_Stop.csproj`:

- `ApplicationDisplayVersion` — the user-facing version, e.g. `1.0`.
- `ApplicationVersion` — the integer version **code**; must increase every upload (1, 2, 3, …).

## 3. Build a signed App Bundle (.aab)

Play requires an `.aab`, not an `.apk`.

```bash
dotnet publish Nicotine_Stop.csproj -f net10.0-android -c Release \
  -p:AndroidPackageFormat=aab \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=snusstop-upload.keystore \
  -p:AndroidSigningKeyAlias=snusstop \
  -p:AndroidSigningKeyPass=env:SNUS_KEY_PASS \
  -p:AndroidSigningStorePass=env:SNUS_STORE_PASS
```

Set `SNUS_KEY_PASS` / `SNUS_STORE_PASS` as environment variables first so the passwords stay out of
your shell history. The bundle lands in:

```
bin/Release/net10.0-android/publish/dk.snusstop.app-Signed.aab
```

## 4. Play Console checklist

- **Package name:** `dk.snusstop.app` (permanent once published).
- **Target API:** the build targets the latest installed platform (API 36); Play's minimum-target
  requirement is met. Min SDK is **API 24**.
- Upload the `.aab` to a **Closed testing** track first, then promote to Production.
- Store listing needs: app icon (512×512), feature graphic (1024×500), 2–8 phone screenshots,
  short + full description, privacy policy URL.
- **Data safety form:** all data is stored **locally on the device**; the app collects nothing and
  sends nothing to a server. Declare "No data collected / No data shared."
- **Health/medical:** describe it as a habit-tracking / motivation app (not medical advice).

## Known dependency advisory (NU1903)

`sqlite-net-pcl` pulls in `SQLitePCLRaw.bundle_green` → `SQLitePCLRaw.lib.e_sqlite3.android`, whose
2.1.11 build carries advisory **GHSA-2m69-gcr7-jv3q** in the bundled native SQLite. It is **not
reachable in this app**: SnusStop stores a single local DB, runs only its own parameterised queries,
and never opens untrusted database files or SQL — the advisory requires attacker-controlled SQL/DB
input. Track SQLitePCLRaw for a patched release and bump when available. To eliminate it now you can
switch to the OS-provided SQLite (`SQLitePCLRaw.bundle_sqlite3`) instead of the bundled engine.

## 5. Local release smoke test

```bash
dotnet build Nicotine_Stop.csproj -f net10.0-android -c Release -t:Run
```

Run through: onboarding → home ticks → beat a craving in the SOS flow → log a slip (streak resets,
money/badges kept) → add a goal → export CSV → add both home-screen widgets.
