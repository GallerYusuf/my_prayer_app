# Android Yayın Rehberi

## Kimlik

| Alan | Değer | Kaynak |
|---|---|---|
| Package ID | `com.galleryusuf.ezanvakti` | `Directory.Build.props` → `PrayerAppApplicationId` |
| Görünen ad | Ezan Vakti | `PrayerAppDisplayName` |
| Version name | 1.0.0 | `PrayerAppDisplayVersion` |
| Version code | 1 | `PrayerAppVersionCode` |

Tüm marka/kimlik değerleri **tek yerden** (`Directory.Build.props`) yönetilir.

### Package ID değişikliğinin etkileri

- Eski `com.companyname.prayerapp` **hiç yayımlanmadığı için** migration gerekmez; Play'de
  yeni uygulama olarak açılır.
- Cihazda eski debug paketi kuruluysa yeni paket **ayrı uygulama** olarak kurulur; eski
  Preferences/SQLite verisi taşınmaz (kabul edilmiş davranış, üretim kullanıcısı yok).
- Package ID **ilk Play yüklemesinden sonra bir daha asla değiştirilemez.**
- Uygulama içi legacy ayar migration'ı (aynı paket içinde eski JSON şema → yeni şema)
  `SettingsStore.TryMigrateLegacySettings` ile korunmuştur.

## Version stratejisi

- `versionCode` her mağaza yüklemesinde +1 (CI'da elle artırılır).
- `versionName` SemVer: MAJOR.MINOR.PATCH (kullanıcıya görünür).

## Signing

Keystore **repoya asla konmaz** (`.gitignore` `*.keystore`, `*.jks` engeller).

### Keystore oluşturma (bir kez)

```bash
keytool -genkeypair -v -keystore ezanvakti-release.keystore -alias ezanvakti `
  -keyalg RSA -keysize 4096 -validity 10000
```

Keystore ve parolaları güvenli bir parola yöneticisinde saklayın.
**Play App Signing** kullanmanız önerilir (Google anahtarı yönetir; upload key kaybolursa
sıfırlanabilir).

### Yerel imzalı AAB üretimi

```powershell
dotnet publish PrayerApp.csproj -f net10.0-android -c Release `
  -p:AndroidPackageFormats=aab `
  -p:AndroidKeyStore=true `
  -p:AndroidSigningKeyStore=C:\keys\ezanvakti-release.keystore `
  -p:AndroidSigningKeyAlias=ezanvakti `
  -p:AndroidSigningKeyPass=env:KEY_PASS `
  -p:AndroidSigningStorePass=env:STORE_PASS
```

Çıktı: `bin\Release\net10.0-android\publish\com.galleryusuf.ezanvakti-Signed.aab`

### CI üzerinden imzalama (GitHub Secrets)

Gerekli secrets: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASS`.

```yaml
- name: Decode keystore
  run: |
    echo "${{ secrets.ANDROID_KEYSTORE_BASE64 }}" | base64 -d > release.keystore
- name: Publish signed AAB
  run: >
    dotnet publish PrayerApp.csproj -f net10.0-android -c Release
    -p:AndroidPackageFormats=aab
    -p:AndroidKeyStore=true
    -p:AndroidSigningKeyStore=release.keystore
    -p:AndroidSigningKeyAlias=${{ secrets.ANDROID_KEY_ALIAS }}
    -p:AndroidSigningKeyPass=${{ secrets.ANDROID_KEY_PASS }}
    -p:AndroidSigningStorePass=${{ secrets.ANDROID_KEYSTORE_PASS }}
```

## Manifest / politika kontrol listesi

- [x] `SCHEDULE_EXACT_ALARM` var, `USE_EXACT_ALARM` yok (bkz. DECISIONS D3)
- [x] `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` kaldırıldı
- [x] `allowBackup=false` (bilinçli karar, DECISIONS D5)
- [x] Konum izinleri yalnızca vakit/kıble için; Play Console'da "location" kullanım gerekçesi
      olarak "core functionality: prayer times" beyan edilecek
- [x] Tüm endpointler HTTPS
- [x] Deep link yok
- [ ] Play Console → App content → Data Safety formu (aşağıdaki taslağa göre)

## Data Safety taslağı

| Soru | Cevap |
|---|---|
| Veri topluyor musunuz? | Hayır (cihaz dışına kişisel veri gönderilmez) |
| Veri paylaşıyor musunuz? | Konum (yaklaşık) yalnızca vakit hesaplama isteğinde Aladhan API'ye iletilir; saklanmaz, reklam/analitik amaçlı kullanılmaz |
| Şifreleme | Aktarımda TLS |
| Silme talebi | Uygulama verisi cihazda; uygulamayı silmek tüm veriyi siler |

## Sürüm notu şablonu

```
Sürüm X.Y.Z
• Yenilik 1
• İyileştirme 2
• Hata düzeltmesi 3
```

## Ekran görüntüsü kontrol listesi

- [ ] Ana sayfa (geri sayım + vakit listesi) — açık ve koyu tema
- [ ] Vakit takvimi
- [ ] Kıble pusulası
- [ ] Kur'an okuma ekranı
- [ ] Zikirmatik
- [ ] Onboarding ilk ekran
- Cihazlar: telefon (1080×2400) zorunlu; 7" ve 10" tablet önerilir

## Yayın öncesi son adımlar

1. `dotnet test` her iki test projesi yeşil
2. `dotnet publish ... -c Release` (trimming dahil) sorunsuz
3. Release APK/AAB fiziksel cihazda smoke test (bkz. `docs/TEST_REPORT.md` senaryoları)
4. `docs/PRAYER_TIME_VALIDATION.md` referans tablosu doldurulmuş
5. Meal lisansı teyidi (bkz. `docs/CONTENT_SOURCES.md`)
6. Privacy policy URL'i yayında (bkz. `docs/PRIVACY_POLICY_TR.md`)
