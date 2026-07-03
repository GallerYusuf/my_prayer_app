# Yol Haritası

## v1.0 (bu sürüm)
- [x] Offline-first vakit motoru (SQLite, 35 gün)
- [x] Güvenilir bildirim sistemi (exact alarm, reboot/timezone dayanıklı)
- [x] Onboarding, takvim, kıble, Kur'an, zikirmatik
- [x] Light/Dark/System tema
- [ ] Diyanet referans karşılaştırması (manuel doğrulama — yayın öncesi)
- [ ] Meal lisans teyidi

## v1.1
- [ ] İl bazlı varsayılan temkin/offset profilleri
- [ ] Ezan sesi (lisanslı asset bulunursa) + kanal sesi seçimi
- [ ] Aylık takvim görünümü (grid)
- [ ] Widget'ın yeni veri katmanından beslenmesi ve tema uyumu
- [ ] İngilizce localization (`AppStrings.en.resx`)

## v1.2
- [ ] iOS yayın hazırlığı (bildirim limitleri, TestFlight)
- [ ] Esmaü'l-Hüsna ve günlük dua bölümü
- [ ] Namaz takibi istatistik ekranı
- [ ] Tablet/geniş ekran düzenleri

## Teknik borç
- [ ] sqlite-net 1.11 stabil çıkınca SQLitePCLRaw 3.x'e geçiş (CVE suppress kaldırılır)
- [ ] UI metinlerinin tamamının AppStrings.resx'e taşınması (kısmi başlandı)
- [ ] Emoji glyph'lerinin (vakit kartları) vector ikonlarla değiştirilmesi
- [ ] MainViewModel'in countdown/verse/tracking alt-VM'lere bölünmesi
