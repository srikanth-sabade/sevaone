namespace SevaOne.Domain.Reference;

/// <summary>
/// Canonical Karnataka district -> taluk list, for validating a Superadmin's district/taluk
/// assignment when creating a gov-admin account (GovAdminManagementService) -- NOT used anywhere
/// in the citizen-facing data path, where district/taluk stay deliberately free-text (DigiLocker
/// KYC data has no canonical spelling enforced -- see SESSION_HANDOFF.md). Admin *assignment* is
/// data a Superadmin enters fresh through a dropdown, so canonicalizing it avoids the same
/// matching-drift risk from the start.
///
/// Source: Wikipedia's "List of taluks of Karnataka" (fetched 2026-09-15), district names
/// normalized to the spellings already used elsewhere in this codebase (matching
/// admin-panel/public/karnataka-districts.json's district list) -- e.g. Wikipedia's
/// "Bagalkote"/"Koppala"/"Chamarajanagara"/"Raichuru"/"Yadagiri" become "Bagalkot"/"Koppal"/
/// "Chamarajanagar"/"Raichur"/"Yadgir" here. Karnataka periodically creates new taluks (last
/// major wave: Vijayanagara's split from Ballari in 2021) -- treat this as a snapshot, not an
/// immutable source of truth; re-verify against an official source before assuming it's exhaustive.
/// </summary>
public static class KarnatakaAdministrativeUnits
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DistrictTaluks = new Dictionary<string, IReadOnlyList<string>>
    {
        ["Bagalkot"] = new[] { "Bagalkote", "Jamkhandi", "Mudhola", "Badami", "Bilagi", "Hunagunda", "Ilkal", "Rabkavi Banhatti", "Guledgudda" },
        ["Ballari"] = new[] { "Ballari", "Kurugodu", "Kampli", "Sanduru", "Siraguppa" },
        ["Belagavi"] = new[] { "Belagavi", "Athani", "Bailhongal", "Chikkodi", "Gokak", "Khanapura", "Mudalgi", "Nippani", "Rayabaga", "Savadatti", "Ramadurga", "Kagawada", "Hukkeri", "Kitturu", "Yargatti" },
        ["Bengaluru Urban"] = new[] { "Bengaluru", "Kengeri", "Krishnarajapura", "Anekal", "Yelahanka" },
        ["Bengaluru Rural"] = new[] { "Nelamangala", "Doddaballapura", "Devanahalli", "Hosakote" },
        ["Bidar"] = new[] { "Aurad", "Basavakalyana", "Bhalki", "Bidar", "Chitgoppa", "Hulsuru", "Humnabad", "Kamalanagara" },
        ["Chamarajanagar"] = new[] { "Chamarajanagara", "Gundlupete", "Kollegala", "Yelanduru", "Hanuru" },
        ["Chikkaballapur"] = new[] { "Chikkaballapura", "Bagepalli", "Chintamani", "Gauribidanuru", "Gudibanda", "Sidlaghatta", "Cheluru", "Manchenahalli" },
        ["Chikkamagaluru"] = new[] { "Chikkamagaluru", "Kaduru", "Koppa", "Mudigere", "Narasimharajapura", "Sringeri", "Tarikere", "Ajjampura", "Kalasa" },
        ["Chitradurga"] = new[] { "Chitradurga", "Challakere", "Hiriyur", "Holalkere", "Hosadurga", "Molakalmuru" },
        ["Dakshina Kannada"] = new[] { "Mangaluru", "Ullal", "Mulki", "Moodbidri", "Bantwala", "Belathangadi", "Putturu", "Sulya", "Kadaba" },
        ["Davanagere"] = new[] { "Davanagere", "Harihara", "Channagiri", "Honnali", "Nyamathi", "Jagaluru" },
        ["Dharwad"] = new[] { "Kalghatgi", "Dharwad", "Hubballi (Rural)", "Hubballi (Urban)", "Kundagolu", "Navalgunda", "Alnavara", "Annigeri" },
        ["Gadag"] = new[] { "Gadag", "Naragunda", "Mundaragi", "Rona", "Gajendragada", "Lakshmeshwara", "Shirahatti" },
        ["Hassan"] = new[] { "Hassan", "Arasikere", "Channarayapattana", "Holenarsipura", "Sakleshpura", "Aluru", "Arakalagudu", "Beluru" },
        ["Haveri"] = new[] { "Ranibennur", "Byadgi", "Hangala", "Haveri", "Savanuru", "Hirekeruru", "Shiggavi", "Rattihalli" },
        ["Kalaburagi"] = new[] { "Kalaburagi", "Afzalpura", "Alanda", "Chincholi", "Chitapura", "Jevargi", "Sedam", "Kamalapura", "Shahabad", "Kalgi", "Yedrami" },
        ["Kodagu"] = new[] { "Madikeri", "Somawarapete", "Virajapete", "Ponnammapete", "Kushalnagara" },
        ["Kolar"] = new[] { "Kolar", "Bangarapete", "Maluru", "Mulabagilu", "Srinivasapura", "Kolar Gold Fields" },
        ["Koppal"] = new[] { "Koppala", "Gangavathi", "Kushtagi", "Yelaburga", "Kanakagiri", "Karatagi", "Kukanuru" },
        ["Mandya"] = new[] { "Mandya", "Madduru", "Malavalli", "Srirangapattana", "Krishnarajapete", "Nagamangala", "Pandavapura" },
        ["Mysuru"] = new[] { "Mysuru", "Hunasuru", "Krishnarajanagara", "Nanjanagodu", "Heggadadevanakote", "Piriyapattana", "Tirumakudalu Narasipura", "Saraguru", "Saligrama" },
        ["Raichur"] = new[] { "Raichuru", "Sindhanuru", "Manvi", "Devadurga", "Lingasaguru", "Mudgal", "Maski", "Sirawara" },
        ["Ramanagara"] = new[] { "Ramanagara", "Magadi", "Kanakapura", "Channapattana", "Harohalli" },
        ["Shivamogga"] = new[] { "Shivamogga", "Sagara", "Bhadravathi", "Hosanagara", "Shikaripura", "Soraba", "Tirthahalli" },
        ["Tumakuru"] = new[] { "Tumakuru", "Chikkanayakanahalli", "Kunigal", "Madhugiri", "Sira", "Tipturu", "Gubbi", "Koratagere", "Pavagada", "Turuvekere" },
        ["Udupi"] = new[] { "Udupi", "Kapu", "Bynduru", "Karkala", "Kundapura", "Hebri", "Brahmavara" },
        ["Uttara Kannada"] = new[] { "Karwara", "Sirsi", "Joida", "Dandeli", "Bhatkal", "Kumta", "Ankola", "Haliyal", "Honnavara", "Mundagodu", "Siddapura", "Yellapura" },
        ["Vijayapura"] = new[] { "Vijayapura", "Indi", "Basavana Bagewadi", "Sindgi", "Muddebihala", "Talikote", "Devara Hipparagi", "Chadchana", "Tikote", "Babaleshwara", "Kolhara", "Nidagundi", "Alamela" },
        ["Yadgir"] = new[] { "Yadagiri", "Shahapura", "Surapura", "Gurmitkala", "Vadagera", "Hunsagi" },
        ["Vijayanagara"] = new[] { "Hosapete", "Hagaribommanahalli", "Harapanahalli", "Hoovina Hadagali", "Kudligi", "Kotturu" },
    };

    public static bool IsValidDistrict(string district) => DistrictTaluks.ContainsKey(district);

    public static bool IsValidTaluk(string district, string taluk) =>
        DistrictTaluks.TryGetValue(district, out var taluks) && taluks.Contains(taluk, StringComparer.OrdinalIgnoreCase);
}
