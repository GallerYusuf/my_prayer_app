# Bildirim Test Rehberi

Cihaz/emülatörde `adb` ile senaryo testleri. Paket: `com.galleryusuf.ezanvakti`

## Hazırlık

```bash
adb shell settings put global auto_time 1
adb logcat -s "Plugin.LocalNotification" "*:E"   # log izleme (ayrı terminalde)
```

## 1. Temel akış

1. Uygulamayı aç, onboarding'i tamamla, bildirim iznini ver.
2. Ayarlar → "Test Bildirimi" → anında bildirim gelmeli.
3. Ayarlar → Tanılama bölümünde "Planlanmış bildirim" sayısı > 0 olmalı.

## 2. Uygulama kapalıyken (en kritik senaryo)

```bash
# Yaklaşan vakti bekleyecek şekilde planla, sonra uygulamayı tamamen kapat:
adb shell am force-stop com.galleryusuf.ezanvakti
# Vakit geldiğinde bildirim gelmeli (AlarmManager, uygulama süreci gerekmez).
```

## 3. Reboot sonrası

```bash
adb reboot
# Cihaz açıldıktan sonra (uygulama hiç açılmadan) planlı vakit bildirimi gelmeli.
# Plugin'in BOOT_COMPLETED restore receiver'ı alarmları geri yükler.
adb shell dumpsys alarm | grep ezanvakti   # alarmların geri yüklendiğini doğrula
```

## 4. Doze modu

```bash
adb shell dumpsys deviceidle force-idle
# AlarmClock tipi alarmlar Doze'da da tetiklenir; vakit bildirimi gelmeli.
adb shell dumpsys deviceidle unforce
adb shell dumpsys battery reset
```

## 5. Saat / timezone değişimi

```bash
# Timezone değiştir → SystemEventsReceiver planı yeniden kurmalı:
adb shell service call alarm 3 s16 Europe/Berlin
adb shell dumpsys alarm | grep ezanvakti   # yeni saatlere göre alarm görülmeli
adb shell service call alarm 3 s16 Europe/Istanbul

# Manuel saat değişimi:
adb shell settings put global auto_time 0
adb shell date 070515002026.00   # TIME_SET broadcast tetiklenir
adb shell settings put global auto_time 1
```

## 6. İzin reddi senaryoları

```bash
# Bildirim iznini geri al:
adb shell pm revoke com.galleryusuf.ezanvakti android.permission.POST_NOTIFICATIONS
# Uygulama çökmemeli; Ayarlar → Tanılama "Bildirim izni: Kapalı" göstermeli.

# Exact alarm iznini kapat (Android 12+): Sistem Ayarları → Alarmlar ve hatırlatıcılar → kapat.
# Tanılama "Tam zamanlı alarm: Kapalı" göstermeli; bildirimler inexact devam etmeli.
```

## 7. Uygulama güncellemesi

```bash
dotnet build -f net10.0-android -c Debug -t:Install
# MY_PACKAGE_REPLACED sonrası alarmlar korunmalı:
adb shell dumpsys alarm | grep ezanvakti
```

## 8. Pil tasarrufu modu

```bash
adb shell settings put global low_power 1
# Vakit bildirimi yine gelmeli (AlarmClock modu). Test sonrası:
adb shell settings put global low_power 0
```

## Kontrol listesi

- [ ] Foreground'da vakit bildirimi
- [ ] Background'da vakit bildirimi
- [ ] Force-stop sonrası vakit bildirimi
- [ ] Reboot sonrası (uygulama açılmadan) bildirim
- [ ] Doze'da bildirim
- [ ] Bildirim izni reddedilince çökme yok + tanılama doğru
- [ ] Exact alarm izni kapalıyken bildirim (inexact) geliyor
- [ ] Timezone değişince plan güncelleniyor
- [ ] Saat değişince plan güncelleniyor
- [ ] Ayar değişince eski bildirimler temizlenip yenisi kuruluyor
- [ ] Ayet bildirimine dokununca ilgili ayet açılıyor
