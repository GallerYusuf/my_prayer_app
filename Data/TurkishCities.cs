using System.Globalization;
using System.Text;
using PrayerApp.Models;

namespace PrayerApp.Data;

public static class TurkishCities
{
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private static readonly TurkishCity[] All = BuildCities();

    private static readonly Dictionary<int, TurkishCity> ByPlate = All.ToDictionary(c => c.PlateCode);
    private static readonly Dictionary<string, TurkishCity> ByApiName = All.ToDictionary(
        c => c.ApiName,
        StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<TurkishCity> GetAll() => All;

    public static TurkishCity GetDefault() => ByPlate[65];

    public static TurkishCity? FindByPlate(int plateCode) =>
        ByPlate.TryGetValue(plateCode, out var city) ? city : null;

    public static TurkishCity? FindByApiName(string? apiName)
    {
        if (string.IsNullOrWhiteSpace(apiName))
            return null;

        if (ByApiName.TryGetValue(apiName.Trim(), out var city))
            return city;

        var normalized = NormalizeForSearch(apiName);
        return All.FirstOrDefault(c =>
            NormalizeForSearch(c.ApiName) == normalized ||
            NormalizeForSearch(c.DisplayName) == normalized);
    }

    public static TurkishDistrict? FindDistrict(TurkishCity? city, string? districtApiName)
    {
        if (city is null || string.IsNullOrWhiteSpace(districtApiName))
            return null;

        var normalized = NormalizeForSearch(districtApiName);
        return city.Districts.FirstOrDefault(d =>
            NormalizeForSearch(d.ApiName) == normalized ||
            NormalizeForSearch(d.DisplayName) == normalized);
    }

    /// <summary>Türkçe karakterleri ASCII karşılıklarına çevirir, büyük/küçük harf ve boşlukları korur.</summary>
    public static string ToAsciiName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim())
        {
            builder.Append(ch switch
            {
                'ı' => 'i',
                'İ' => 'I',
                'ğ' => 'g',
                'Ğ' => 'G',
                'ü' => 'u',
                'Ü' => 'U',
                'ş' => 's',
                'Ş' => 'S',
                'ö' => 'o',
                'Ö' => 'O',
                'ç' => 'c',
                'Ç' => 'C',
                _ => ch
            });
        }

        return builder.ToString();
    }

    public static string NormalizeForSearch(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var lowered = value.Trim().ToLower(TurkishCulture);
        var builder = new StringBuilder(lowered.Length);

        foreach (var ch in lowered)
        {
            builder.Append(ch switch
            {
                'ı' => 'i',
                'ğ' => 'g',
                'ü' => 'u',
                'ş' => 's',
                'ö' => 'o',
                'ç' => 'c',
                _ => ch
            });
        }

        return builder.ToString();
    }

    public static bool MatchesSearch(TurkishCity city, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        var q = NormalizeForSearch(query);

        return NormalizeForSearch(city.DisplayName).Contains(q, StringComparison.Ordinal)
            || NormalizeForSearch(city.ApiName).Contains(q, StringComparison.Ordinal)
            || NormalizeForSearch(city.Region).Contains(q, StringComparison.Ordinal)
            || city.PlateLabel.Contains(q, StringComparison.Ordinal);
    }

    public static bool MatchesSearch(TurkishDistrict district, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        var q = NormalizeForSearch(query);

        return NormalizeForSearch(district.DisplayName).Contains(q, StringComparison.Ordinal)
            || NormalizeForSearch(district.ApiName).Contains(q, StringComparison.Ordinal);
    }

    private static TurkishCity City(int plate, string display, string region, params string[] districtNames)
    {
        var districts = districtNames
            .OrderBy(n => n, StringComparer.Create(TurkishCulture, ignoreCase: true))
            .Select(n => new TurkishDistrict
            {
                PlateCode = plate,
                DisplayName = n,
                ApiName = ToAsciiName(n)
            })
            .ToArray();

        return new TurkishCity
        {
            PlateCode = plate,
            DisplayName = display,
            ApiName = ToAsciiName(display),
            Region = region,
            Districts = districts
        };
    }

    private static TurkishCity[] BuildCities() =>
    [
        City(1, "Adana", "Akdeniz", "Seyhan", "Yüreğir", "Çukurova", "Sarıçam", "Ceyhan", "Kozan", "İmamoğlu", "Karaisalı", "Karataş", "Pozantı", "Saimbeyli", "Aladağ", "Feke", "Tufanbeyli", "Yumurtalık"),
        City(2, "Adıyaman", "Güneydoğu Anadolu", "Besni", "Kâhta", "Gölbaşı", "Gerger", "Çelikhan", "Samsat", "Sincik", "Tut"),
        City(3, "Afyonkarahisar", "Ege", "Sandıklı", "Dinar", "Bolvadin", "Emirdağ", "Sinanpaşa", "Şuhut", "Çay", "İhsaniye", "Sultandağı", "Çobanlar", "Hocalar", "İscehisar", "Başmakçı", "Bayat", "Dazkırı", "Evciler", "Kızılören"),
        City(4, "Ağrı", "Doğu Anadolu", "Patnos", "Doğubayazıt", "Diyadin", "Eleşkirt", "Taşlıçay", "Tutak", "Hamur"),
        City(5, "Amasya", "Karadeniz", "Merzifon", "Suluova", "Taşova", "Gümüşhacıköy", "Göynücek", "Hamamözü"),
        City(6, "Ankara", "İç Anadolu", "Çankaya", "Keçiören", "Yenimahalle", "Mamak", "Etimesgut", "Sincan", "Altındağ", "Pursaklar", "Gölbaşı", "Polatlı", "Çubuk", "Kahramankazan", "Beypazarı", "Elmadağ", "Akyurt", "Nallıhan", "Kızılcahamam", "Şereflikoçhisar", "Haymana", "Bala", "Ayaş", "Güdül", "Çamlıdere", "Kalecik", "Evren"),
        City(7, "Antalya", "Akdeniz", "Muratpaşa", "Kepez", "Konyaaltı", "Alanya", "Manavgat", "Serik", "Kumluca", "Kaş", "Korkuteli", "Aksu", "Döşemealtı", "Finike", "Gazipaşa", "Kemer", "Elmalı", "Demre", "Akseki", "Gündoğmuş", "İbradı"),
        City(8, "Artvin", "Karadeniz", "Hopa", "Borçka", "Arhavi", "Yusufeli", "Şavşat", "Ardanuç", "Murgul", "Kemalpaşa"),
        City(9, "Aydın", "Ege", "Efeler", "Nazilli", "Söke", "Kuşadası", "Didim", "Çine", "Germencik", "İncirliova", "Köşk", "Bozdoğan", "Karacasu", "Kuyucak", "Sultanhisar", "Yenipazar", "Buharkent", "Karpuzlu", "Koçarlı"),
        City(10, "Balıkesir", "Marmara", "Altıeylül", "Karesi", "Bandırma", "Edremit", "Gönen", "Ayvalık", "Burhaniye", "Bigadiç", "Susurluk", "Erdek", "Sındırgı", "Dursunbey", "Manyas", "Havran", "Savaştepe", "Kepsut", "İvrindi", "Gömeç", "Balya", "Marmara"),
        City(11, "Bilecik", "Marmara", "Bozüyük", "Söğüt", "Osmaneli", "Gölpazarı", "Pazaryeri", "Yenipazar", "İnhisar"),
        City(12, "Bingöl", "Doğu Anadolu", "Genç", "Solhan", "Karlıova", "Adaklı", "Kiğı", "Yedisu", "Yayladere"),
        City(13, "Bitlis", "Doğu Anadolu", "Tatvan", "Güroymak", "Ahlat", "Hizan", "Mutki", "Adilcevaz"),
        City(14, "Bolu", "Karadeniz", "Gerede", "Mengen", "Mudurnu", "Göynük", "Yeniçağa", "Dörtdivan", "Kıbrıscık", "Seben"),
        City(15, "Burdur", "Akdeniz", "Bucak", "Gölhisar", "Yeşilova", "Ağlasun", "Tefenni", "Çavdır", "Karamanlı", "Çeltikçi", "Altınyayla", "Kemer"),
        City(16, "Bursa", "Marmara", "Osmangazi", "Yıldırım", "Nilüfer", "İnegöl", "Gemlik", "Mudanya", "Mustafakemalpaşa", "Gürsu", "Karacabey", "Orhangazi", "Kestel", "Yenişehir", "Orhaneli", "Keles", "Harmancık", "Büyükorhan"),
        City(17, "Çanakkale", "Marmara", "Biga", "Çan", "Gelibolu", "Yenice", "Ezine", "Bayramiç", "Lapseki", "Ayvacık", "Eceabat", "Bozcaada", "Gökçeada"),
        City(18, "Çankırı", "İç Anadolu", "Çerkeş", "Ilgaz", "Şabanözü", "Orta", "Kurşunlu", "Yapraklı", "Kızılırmak", "Eldivan", "Atkaracalar", "Korgun", "Bayramören"),
        City(19, "Çorum", "Karadeniz", "Sungurlu", "Osmancık", "İskilip", "Alaca", "Bayat", "Kargı", "Mecitözü", "Ortaköy", "Uğurludağ", "Oğuzlar", "Dodurga", "Laçin", "Boğazkale"),
        City(20, "Denizli", "Ege", "Pamukkale", "Merkezefendi", "Çivril", "Acıpayam", "Tavas", "Honaz", "Sarayköy", "Buldan", "Çal", "Kale", "Çameli", "Bekilli", "Güney", "Serinhisar", "Bozkurt", "Babadağ", "Beyağaç", "Baklan"),
        City(21, "Diyarbakır", "Güneydoğu Anadolu", "Bağlar", "Kayapınar", "Yenişehir", "Sur", "Ergani", "Bismil", "Silvan", "Çınar", "Çermik", "Dicle", "Kulp", "Hani", "Lice", "Hazro", "Kocaköy", "Eğil", "Çüngüş"),
        City(22, "Edirne", "Marmara", "Keşan", "Uzunköprü", "İpsala", "Havsa", "Enez", "Meriç", "Süloğlu", "Lalapaşa"),
        City(23, "Elazığ", "Doğu Anadolu", "Kovancılar", "Karakoçan", "Palu", "Maden", "Baskil", "Arıcak", "Sivrice", "Keban", "Ağın", "Alacakaya"),
        City(24, "Erzincan", "Doğu Anadolu", "Tercan", "Üzümlü", "Refahiye", "Çayırlı", "İliç", "Kemah", "Kemaliye", "Otlukbeli"),
        City(25, "Erzurum", "Doğu Anadolu", "Yakutiye", "Palandöken", "Aziziye", "Horasan", "Oltu", "Pasinler", "Karayazı", "Hınıs", "Tekman", "Şenkaya", "Karaçoban", "Çat", "Aşkale", "Köprüköy", "Narman", "Tortum", "Olur", "Uzundere", "İspir", "Pazaryolu"),
        City(26, "Eskişehir", "İç Anadolu", "Odunpazarı", "Tepebaşı", "Sivrihisar", "Çifteler", "Mahmudiye", "Seyitgazi", "Alpu", "Beylikova", "İnönü", "Günyüzü", "Han", "Mihalıççık", "Sarıcakaya", "Mihalgazi"),
        City(27, "Gaziantep", "Güneydoğu Anadolu", "Şahinbey", "Şehitkamil", "Nizip", "İslahiye", "Nurdağı", "Araban", "Oğuzeli", "Yavuzeli", "Karkamış"),
        City(28, "Giresun", "Karadeniz", "Bulancak", "Espiye", "Tirebolu", "Görele", "Dereli", "Şebinkarahisar", "Keşap", "Yağlıdere", "Piraziz", "Eynesil", "Doğankent", "Güce", "Çamoluk", "Çanakçı", "Alucra"),
        City(29, "Gümüşhane", "Karadeniz", "Kelkit", "Şiran", "Köse", "Torul", "Kürtün"),
        City(30, "Hakkari", "Doğu Anadolu", "Yüksekova", "Şemdinli", "Çukurca", "Derecik"),
        City(31, "Hatay", "Akdeniz", "Antakya", "İskenderun", "Defne", "Dörtyol", "Samandağ", "Kırıkhan", "Reyhanlı", "Arsuz", "Payas", "Erzin", "Hassa", "Altınözü", "Belen", "Kumlu", "Yayladağı"),
        City(32, "Isparta", "Akdeniz", "Yalvaç", "Eğirdir", "Şarkikaraağaç", "Gelendost", "Senirkent", "Keçiborlu", "Sütçüler", "Atabey", "Gönen", "Uluborlu", "Aksu", "Yenişarbademli"),
        City(33, "Mersin", "Akdeniz", "Tarsus", "Toroslar", "Akdeniz", "Yenişehir", "Mezitli", "Erdemli", "Silifke", "Anamur", "Mut", "Bozyazı", "Gülnar", "Aydıncık", "Çamlıyayla"),
        City(34, "İstanbul", "Marmara", "Esenyurt", "Küçükçekmece", "Bağcılar", "Pendik", "Ümraniye", "Bahçelievler", "Sultangazi", "Maltepe", "Gaziosmanpaşa", "Esenler", "Kartal", "Kâğıthane", "Sancaktepe", "Eyüpsultan", "Avcılar", "Sarıyer", "Başakşehir", "Beylikdüzü", "Kadıköy", "Zeytinburnu", "Üsküdar", "Çekmeköy", "Şişli", "Güngören", "Bayrampaşa", "Fatih", "Tuzla", "Beykoz", "Arnavutköy", "Sultanbeyli", "Ataşehir", "Beyoğlu", "Büyükçekmece", "Silivri", "Çatalca", "Bakırköy", "Beşiktaş", "Şile", "Adalar"),
        City(35, "İzmir", "Ege", "Buca", "Karabağlar", "Bornova", "Konak", "Karşıyaka", "Bayraklı", "Çiğli", "Torbalı", "Menemen", "Gaziemir", "Ödemiş", "Kemalpaşa", "Bergama", "Menderes", "Tire", "Aliağa", "Urla", "Narlıdere", "Balçova", "Kiraz", "Foça", "Dikili", "Bayındır", "Seferihisar", "Selçuk", "Güzelbahçe", "Çeşme", "Beydağ", "Karaburun", "Kınık"),
        City(36, "Kars", "Doğu Anadolu", "Kağızman", "Sarıkamış", "Selim", "Digor", "Arpaçay", "Akyaka", "Susuz"),
        City(37, "Kastamonu", "Karadeniz", "Tosya", "Taşköprü", "Cide", "İnebolu", "Araç", "Bozkurt", "Çatalzeytin", "Devrekani", "Daday", "Doğanyurt", "Küre", "Pınarbaşı", "Şenpazar", "Ağlı", "Abana", "Hanönü", "İhsangazi", "Seydiler", "Azdavay"),
        City(38, "Kayseri", "İç Anadolu", "Melikgazi", "Kocasinan", "Talas", "Develi", "Yahyalı", "Bünyan", "İncesu", "Pınarbaşı", "Tomarza", "Yeşilhisar", "Sarıoğlan", "Sarız", "Akkışla", "Felahiye", "Hacılar", "Özvatan"),
        City(39, "Kırklareli", "Marmara", "Lüleburgaz", "Babaeski", "Vize", "Pınarhisar", "Demirköy", "Pehlivanköy", "Kofçaz"),
        City(40, "Kırşehir", "İç Anadolu", "Kaman", "Mucur", "Çiçekdağı", "Akpınar", "Boztepe", "Akçakent"),
        City(41, "Kocaeli", "Marmara", "Gebze", "İzmit", "Darıca", "Körfez", "Gölcük", "Derince", "Çayırova", "Kartepe", "Başiskele", "Karamürsel", "Dilovası", "Kandıra"),
        City(42, "Konya", "İç Anadolu", "Selçuklu", "Meram", "Karatay", "Ereğli", "Akşehir", "Beyşehir", "Çumra", "Seydişehir", "Ilgın", "Cihanbeyli", "Karapınar", "Kulu", "Bozkır", "Kadınhanı", "Sarayönü", "Doğanhisar", "Hüyük", "Yunak", "Çeltik", "Altınekin", "Hadim", "Taşkent", "Derbent", "Emirgazi", "Güneysınır", "Halkapınar", "Tuzlukçu", "Ahırlı", "Akören", "Derebucak", "Yalıhüyük"),
        City(43, "Kütahya", "Ege", "Tavşanlı", "Simav", "Gediz", "Emet", "Domaniç", "Hisarcık", "Altıntaş", "Aslanapa", "Çavdarhisar", "Pazarlar", "Şaphane", "Dumlupınar"),
        City(44, "Malatya", "Doğu Anadolu", "Battalgazi", "Yeşilyurt", "Doğanşehir", "Akçadağ", "Darende", "Hekimhan", "Arapgir", "Pütürge", "Yazıhan", "Arguvan", "Kuluncak", "Kale", "Doğanyol"),
        City(45, "Manisa", "Ege", "Yunusemre", "Şehzadeler", "Akhisar", "Turgutlu", "Salihli", "Soma", "Alaşehir", "Saruhanlı", "Kula", "Demirci", "Gördes", "Sarıgöl", "Kırkağaç", "Selendi", "Ahmetli", "Gölmarmara", "Köprübaşı"),
        City(46, "Kahramanmaraş", "Akdeniz", "Onikişubat", "Dulkadiroğlu", "Elbistan", "Afşin", "Pazarcık", "Türkoğlu", "Göksun", "Çağlayancerit", "Andırın", "Nurhak", "Ekinözü"),
        City(47, "Mardin", "Güneydoğu Anadolu", "Artuklu", "Kızıltepe", "Midyat", "Nusaybin", "Derik", "Mazıdağı", "Dargeçit", "Savur", "Ömerli", "Yeşilli"),
        City(48, "Muğla", "Ege", "Bodrum", "Fethiye", "Milas", "Menteşe", "Marmaris", "Seydikemer", "Ortaca", "Dalaman", "Yatağan", "Köyceğiz", "Ula", "Datça", "Kavaklıdere"),
        City(49, "Muş", "Doğu Anadolu", "Bulanık", "Malazgirt", "Varto", "Hasköy", "Korkut"),
        City(50, "Nevşehir", "İç Anadolu", "Ürgüp", "Avanos", "Gülşehir", "Derinkuyu", "Acıgöl", "Kozaklı", "Hacıbektaş"),
        City(51, "Niğde", "İç Anadolu", "Bor", "Çiftlik", "Ulukışla", "Altunhisar", "Çamardı"),
        City(52, "Ordu", "Karadeniz", "Altınordu", "Ünye", "Fatsa", "Perşembe", "Korgan", "Kumru", "Gölköy", "Mesudiye", "Aybastı", "Ulubey", "Çamaş", "Çatalpınar", "Çaybaşı", "Gülyalı", "Gürgentepe", "İkizce", "Kabadüz", "Kabataş"),
        City(53, "Rize", "Karadeniz", "Çayeli", "Ardeşen", "Pazar", "Fındıklı", "Güneysu", "Kalkandere", "İyidere", "Derepazarı", "Çamlıhemşin", "Hemşin", "İkizdere"),
        City(54, "Sakarya", "Marmara", "Adapazarı", "Serdivan", "Erenler", "Akyazı", "Hendek", "Karasu", "Geyve", "Pamukova", "Sapanca", "Ferizli", "Söğütlü", "Kaynarca", "Kocaali", "Arifiye", "Karapürçek", "Taraklı"),
        City(55, "Samsun", "Karadeniz", "İlkadım", "Atakum", "Bafra", "Çarşamba", "Vezirköprü", "Terme", "Tekkeköy", "Havza", "Salıpazarı", "Alaçam", "19 Mayıs", "Ayvacık", "Asarcık", "Kavak", "Ladik", "Yakakent", "Canik"),
        City(56, "Siirt", "Güneydoğu Anadolu", "Kurtalan", "Pervari", "Baykan", "Şirvan", "Eruh", "Tillo"),
        City(57, "Sinop", "Karadeniz", "Boyabat", "Gerze", "Ayancık", "Durağan", "Türkeli", "Erfelek", "Dikmen", "Saraydüzü"),
        City(58, "Sivas", "İç Anadolu", "Şarkışla", "Yıldızeli", "Suşehri", "Zara", "Gemerek", "Kangal", "Divriği", "Gürün", "Hafik", "Koyulhisar", "Altınyayla", "İmranlı", "Akıncılar", "Doğanşar", "Gölova", "Ulaş"),
        City(59, "Tekirdağ", "Marmara", "Süleymanpaşa", "Çorlu", "Çerkezköy", "Kapaklı", "Ergene", "Malkara", "Saray", "Hayrabolu", "Muratlı", "Şarköy", "Marmaraereğlisi"),
        City(60, "Tokat", "Karadeniz", "Erbaa", "Turhal", "Niksar", "Zile", "Reşadiye", "Almus", "Pazar", "Yeşilyurt", "Artova", "Başçiftlik", "Sulusaray"),
        City(61, "Trabzon", "Karadeniz", "Ortahisar", "Akçaabat", "Of", "Araklı", "Yomra", "Vakfıkebir", "Arsin", "Sürmene", "Maçka", "Beşikdüzü", "Çarşıbaşı", "Tonya", "Düzköy", "Çaykara", "Şalpazarı", "Hayrat", "Köprübaşı", "Dernekpazarı"),
        City(62, "Tunceli", "Doğu Anadolu", "Pertek", "Çemişgezek", "Mazgirt", "Hozat", "Ovacık", "Pülümür", "Nazımiye"),
        City(63, "Şanlıurfa", "Güneydoğu Anadolu", "Eyyübiye", "Haliliye", "Siverek", "Viranşehir", "Karaköprü", "Suruç", "Akçakale", "Birecik", "Ceylanpınar", "Harran", "Bozova", "Hilvan", "Halfeti"),
        City(64, "Uşak", "Ege", "Banaz", "Eşme", "Sivaslı", "Ulubey", "Karahallı"),
        City(65, "Van", "Doğu Anadolu", "İpekyolu", "Erciş", "Tuşba", "Çaldıran", "Özalp", "Başkale", "Muradiye", "Gevaş", "Gürpınar", "Edremit", "Saray", "Çatak", "Bahçesaray"),
        City(66, "Yozgat", "İç Anadolu", "Sorgun", "Yerköy", "Akdağmadeni", "Boğazlıyan", "Sarıkaya", "Çekerek", "Şefaatli", "Çayıralan", "Saraykent", "Kadışehri", "Aydıncık", "Yenifakılı", "Çandır"),
        City(67, "Zonguldak", "Karadeniz", "Ereğli", "Çaycuma", "Devrek", "Alaplı", "Kilimli", "Kozlu", "Gökçebey"),
        City(68, "Aksaray", "İç Anadolu", "Ortaköy", "Eskil", "Gülağaç", "Güzelyurt", "Ağaçören", "Sarıyahşi", "Sultanhanı"),
        City(69, "Bayburt", "Karadeniz", "Demirözü", "Aydıntepe"),
        City(70, "Karaman", "İç Anadolu", "Ermenek", "Sarıveliler", "Ayrancı", "Kazımkarabekir", "Başyayla"),
        City(71, "Kırıkkale", "İç Anadolu", "Yahşihan", "Keskin", "Delice", "Bahşılı", "Sulakyurt", "Balışeyh", "Çelebi", "Karakeçili"),
        City(72, "Batman", "Güneydoğu Anadolu", "Kozluk", "Sason", "Beşiri", "Gercüş", "Hasankeyf"),
        City(73, "Şırnak", "Güneydoğu Anadolu", "Cizre", "Silopi", "İdil", "Uludere", "Beytüşşebap", "Güçlükonak"),
        City(74, "Bartın", "Karadeniz", "Ulus", "Amasra", "Kurucaşile"),
        City(75, "Ardahan", "Doğu Anadolu", "Göle", "Çıldır", "Hanak", "Posof", "Damal"),
        City(76, "Iğdır", "Doğu Anadolu", "Tuzluca", "Aralık", "Karakoyunlu"),
        City(77, "Yalova", "Marmara", "Çiftlikköy", "Çınarcık", "Altınova", "Armutlu", "Termal"),
        City(78, "Karabük", "Karadeniz", "Safranbolu", "Yenice", "Eskipazar", "Eflani", "Ovacık"),
        City(79, "Kilis", "Güneydoğu Anadolu", "Musabeyli", "Elbeyli", "Polateli"),
        City(80, "Osmaniye", "Akdeniz", "Kadirli", "Düziçi", "Bahçe", "Toprakkale", "Sumbas", "Hasanbeyli"),
        City(81, "Düzce", "Karadeniz", "Akçakoca", "Gümüşova", "Çilimli", "Cumayeri", "Gölyaka", "Yığılca", "Kaynaşlı")
    ];
}
