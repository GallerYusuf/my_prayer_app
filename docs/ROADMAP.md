# Yol Haritası

## v1.0 (bu sürüm)
- [x] Offline-first vakit motoru (SQLite, 35 gün)
- [x] Güvenilir bildirim sistemi (exact alarm, reboot/timezone dayanıklı)
- [x] Onboarding, takvim, kıble, Kur'an, zikirmatik
- [x] Light/Dark/System tema
- [x] Sürekli vakit bildirimi (ongoing, sessiz, kapatılabilir)
- [x] Widget'ın yeni veri katmanından beslenmesi (alarm zinciri + native geri sayım)
- [x] Kur'an: Arapça metin + cüz + kaldığın yerden devam + hatim takibi
- [x] Esmaül Hüsna, Kur'an'dan dualar, dini günler, kaza takibi, zekât, tesbihat, kerahat
- [ ] Diyanet referans karşılaştırması (manuel doğrulama — yayın öncesi)
- [ ] Meal lisans teyidi

## v1.1
- [ ] İl bazlı varsayılan temkin/offset profilleri
- [ ] Ezan sesi (lisanslı asset bulunursa) + kanal sesi seçimi
- [ ] Aylık takvim görünümü (grid)
- [ ] Widget tema uyumu (dinamik renkler / Material You)
- [ ] İngilizce localization (`AppStrings.en.resx`)

## v1.2
- [ ] iOS yayın hazırlığı (bildirim limitleri, TestFlight)
- [x] Esmaü'l-Hüsna ve dua bölümü (v1.0'a öne alındı)
- [ ] Namaz takibi istatistik ekranı
- [ ] Tablet/geniş ekran düzenleri
- [ ] Hatim istatistikleri (tamamlanan hatimler, günlük grafik)

## Teknik borç
- [ ] sqlite-net 1.11 stabil çıkınca SQLitePCLRaw 3.x'e geçiş (CVE suppress kaldırılır)
- [ ] UI metinlerinin tamamının AppStrings.resx'e taşınması (kısmi başlandı)
- [x] Emoji glyph'lerinin vector ikonlarla değiştirilmesi (2026-07-06)
- [ ] MainViewModel'in countdown/verse/tracking alt-VM'lere bölünmesi
