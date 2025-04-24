using FrietBot.Models;

namespace FrietBot.Services;

public interface IMenuService
{
    MenuConfig GetMenuConfig();
    MenuItem? GetMenuItem(string type, string id);
}

public class MenuService : IMenuService
{
    private readonly MenuConfig _menuConfig;

    public MenuService()
    {
        _menuConfig = new MenuConfig
        {
            Friet =
            [
                new MenuItem { Id = "friet_klein", Name = "Kleine Friet" },
                new MenuItem { Id = "friet_klein_speciaal", Name = "Kleine Friet Speciaal" },
                new MenuItem { Id = "friet_klein_speciaal_belgisch", Name = "Kleine Friet Speciaal Belgische Mayo" },
                new MenuItem { Id = "friet_klein_mayo", Name = "Kleine Friet Mayo" },
                new MenuItem { Id = "friet_klein_mayo_belgisch", Name = "Kleine Friet Belgische Mayo" },
                new MenuItem { Id = "friet_klein_curry", Name = "Kleine Friet Curry" },
                new MenuItem { Id = "friet_klein_ketchup", Name = "Kleine Friet Ketchup" },
                new MenuItem { Id = "friet_klein_pinda", Name = "Kleine Friet Pinda" },
                new MenuItem { Id = "friet_klein_oorlog", Name = "Kleine Friet Oorlog" },
                new MenuItem { Id = "friet_klein_piccalilly", Name = "Kleine Friet Piccalilly" },
                new MenuItem { Id = "friet_klein_zigeunersaus", Name = "Kleine Friet Zigeunersaus" },
                new MenuItem { Id = "friet_klein_zuurvlees", Name = "Kleine Friet Zuurvlees" },
                new MenuItem { Id = "friet_klein_goulash", Name = "Kleine Friet Goulash" },
                new MenuItem { Id = "friet_klein_joppiesaus", Name = "Kleine Friet Joppiesaus" },
                new MenuItem { Id = "friet_klein_appelmoes", Name = "Kleine Friet Appelmoes" },

                new MenuItem { Id = "friet", Name = "Friet" },
                new MenuItem { Id = "friet_speciaal", Name = "Friet Speciaal" },
                new MenuItem { Id = "friet_speciaal_belgisch", Name = "Friet Speciaal Belgische Mayo" },
                new MenuItem { Id = "friet_mayo", Name = "Friet Mayo" },
                new MenuItem { Id = "friet_mayo_belgisch", Name = "Friet Belgische Mayo" },
                new MenuItem { Id = "friet_curry", Name = "Friet Curry" },
                new MenuItem { Id = "friet_ketchup", Name = "Friet Ketchup" },
                new MenuItem { Id = "friet_pinda", Name = "Friet Pinda" },
                new MenuItem { Id = "friet_oorlog", Name = "Friet Oorlog" },
                new MenuItem { Id = "friet_piccalilly", Name = "Friet Piccalilly" },
                new MenuItem { Id = "friet_zigeunersaus", Name = "Friet Zigeunersaus" },
                new MenuItem { Id = "friet_zuurvlees", Name = "Friet Zuurvlees" },
                new MenuItem { Id = "friet_goulash", Name = "Friet Goulash" },
                new MenuItem { Id = "friet_joppiesaus", Name = "Friet Joppiesaus" },
                new MenuItem { Id = "friet_appelmoes", Name = "Friet Appelmoes" },

                new MenuItem { Id = "friet_groot", Name = "Grote Friet" },
                new MenuItem { Id = "friet_groot_speciaal", Name = "Grote Friet Speciaal" },
                new MenuItem { Id = "friet_groot_speciaal_belgisch", Name = "Grote Friet Speciaal Belgische Mayo" },
                new MenuItem { Id = "friet_groot_mayo", Name = "Grote Friet Mayo" },
                new MenuItem { Id = "friet_groot_mayo_belgisch", Name = "Grote Friet Belgische Mayo" },
                new MenuItem { Id = "friet_groot_curry", Name = "Grote Friet Curry" },
                new MenuItem { Id = "friet_groot_ketchup", Name = "Grote Friet Ketchup" },
                new MenuItem { Id = "friet_groot_pinda", Name = "Grote Friet Pinda" },
                new MenuItem { Id = "friet_groot_oorlog", Name = "Grote Friet Oorlog" },
                new MenuItem { Id = "friet_groot_piccalilly", Name = "Grote Friet Piccalilly" },
                new MenuItem { Id = "friet_groot_zigeunersaus", Name = "Grote Friet Zigeunersaus" },
                new MenuItem { Id = "friet_groot_zuurvlees", Name = "Grote Friet Zuurvlees" },
                new MenuItem { Id = "friet_groot_zuurvlees_extra", Name = "Grote Friet Zuurvlees Extra" },
                new MenuItem { Id = "friet_groot_goulash", Name = "Grote Friet Goulash" },
                new MenuItem { Id = "friet_groot_goulash_extra", Name = "Grote Friet Goulash Extra" },
                new MenuItem { Id = "friet_groot_joppiesaus", Name = "Grote Friet Joppiesaus" },
                new MenuItem { Id = "friet_groot_appelmoes", Name = "Grote Friet Appelmoes" },
            ],
            Snacks =
            [
                new MenuItem { Id = "frikandel", Name = "Frikandel" },
                new MenuItem { Id = "frikandel_speciaal", Name = "Frikandel speciaal" },
                new MenuItem { Id = "frikandel_pinda", Name = "Frikandel pinda" },
                new MenuItem { Id = "frikandel_pinda_speciaal", Name = "Frikandel pinda speciaal" },
                new MenuItem { Id = "kip_frikandel", Name = "Kip frikandel" },
                new MenuItem { Id = "kroket", Name = "Kroket" },
                new MenuItem { Id = "kroket_goulash", Name = "Goulash kroket" },
                new MenuItem { Id = "kroket_grote_vlees", Name = "Grote vlees kroket" },
                new MenuItem { Id = "kroket_rundvlees", Name = "Rundvleeskroket" },
                new MenuItem { Id = "kroket_kalf", Name = "Kalfskroket" },
                new MenuItem { Id = "kroket_kip_champignon", Name = "Kip-champignonkroket" },
                new MenuItem { Id = "kroket_sate", Name = "Satékroket" },
                new MenuItem { Id = "kroket_kip_sate", Name = "Kipsatékroket" },
                new MenuItem { Id = "kroket_smul", Name = "Smulkroket" },
                new MenuItem { Id = "smulstick", Name = "Smulstick" },

                new MenuItem { Id = "gehaktbal", Name = "Gehaktbal" },
                new MenuItem { Id = "gehaktbal_speciaal", Name = "Gehaktbal speciaal" },
                new MenuItem { Id = "gehaktstaaf", Name = "Gehaktstaaf" },
                new MenuItem { Id = "bami", Name = "Bami" },
                new MenuItem { Id = "nasi", Name = "Nasi" },
                new MenuItem { Id = "bami_extra_pittig", Name = "Bami extra pittig" },
                new MenuItem { Id = "bami_oriental", Name = "Bami oriëntal " },
                new MenuItem { Id = "nasi_oriental", Name = "Nasi oriëntal" },
                new MenuItem { Id = "bocado", Name = "Bocado" },
                new MenuItem { Id = "mexicano", Name = "Grizly" },
                new MenuItem { Id = "grizly", Name = "Satérol" },
                new MenuItem { Id = "saterol", Name = "Smulrol" },
                new MenuItem { Id = "smulrol", Name = "Sitostick" },
                new MenuItem { Id = "sitostick", Name = "Sitostick" },
                new MenuItem { Id = "knakworst", Name = "Knakworst" },
                new MenuItem { Id = "bockworst", Name = "Bockworst" },
                new MenuItem { Id = "vietnamese_loempia", Name = "Vietnamese loempia" },
                new MenuItem { Id = "loempia", Name = "Loempia" },
                new MenuItem { Id = "frikandel_xxl", Name = "XXL frikandel" },
                new MenuItem { Id = "frikandel_xxl_speciaal", Name = "XXL frikandel speciaal" },
                new MenuItem { Id = "twijfelaar", Name = "Twijfelaar" },
                new MenuItem { Id = "loempia_mini", Name = "Mini loempia" },
                new MenuItem { Id = "loempidel", Name = "Loempidel" },
                new MenuItem { Id = "loempia_speciaal", Name = "Loempia spec. (ei + pindasaus)" },
                new MenuItem { Id = "shaslick", Name = "Shaslick + saus" },
                new MenuItem { Id = "kipnuggets_6", Name = "Kipnuggets (6 stuks)" },
                new MenuItem { Id = "kipcorn", Name = "Kipcorn" },
                new MenuItem { Id = "viandel", Name = "Viandel" },
                new MenuItem { Id = "kipfingers", Name = "Kipfingers (6 stuks)" },
                new MenuItem { Id = "sate_kip", Name = "Saté kip" },
                new MenuItem { Id = "sate_haas", Name = "Saté van de haas" },
                new MenuItem { Id = "ham_kaas_souffle", Name = "Ham-kaassoufflé" },
                new MenuItem { Id = "zigeunerstick", Name = "Zigeunerstick + saus" },
                new MenuItem { Id = "shoarmarol", Name = "Shoarmarol + saus" },
                new MenuItem { Id = "curryworst", Name = "Curryworst + saus" },
                new MenuItem { Id = "bitterballen", Name = "Bitterballen (5 stuks)" },
                new MenuItem { Id = "mini_snacks", Name = "Mini snacks (5 stuks)" },
                new MenuItem { Id = "mini_snacks_speciaal", Name = "Mini snacks speciaal (5 stuks)" },
                new MenuItem { Id = "halve_haan", Name = "Halve haan" }
            ],
            Burgers =
            [
                new MenuItem { Id = "hamburger", Name = "Hamburger" },
                new MenuItem { Id = "hamburger_speciaal", Name = "Hamburger speciaal" },
                new MenuItem { Id = "hamburger_hawai", Name = "Hamburger hawaï" },
                new MenuItem { Id = "hamburger_ei", Name = "Hamburger ei" },
                new MenuItem { Id = "hamburger_rundvlees", Name = "Hamburger rundvlees" },
                new MenuItem { Id = "hamburger_smulhoven", Name = "Hamburger Smulhoven" },
                new MenuItem { Id = "kipburger", Name = "Kipburger" },
                new MenuItem { Id = "visburger", Name = "Visburger" },
                new MenuItem { Id = "cheeseburger", Name = "Cheeseburger" },
                new MenuItem { Id = "giant_chicago", Name = "Giant Chicago" },
                new MenuItem { Id = "hamburger_speciaal_nieuw", Name = "Hamburger speciaal nieuw" },
                new MenuItem { Id = "cheeseburger_dubbel", Name = "Dubbele cheeseburger" },
                new MenuItem { Id = "burger_dubbel", Name = "Dubbele burger" }
            ],
            Broodjes =
            [
                new MenuItem { Id = "broodje_ei", Name = "Broodje Ei" },
                new MenuItem { Id = "broodje_kaas", Name = "Broodje Kaas" },
                new MenuItem { Id = "broodje_ham", Name = "Broodje Ham" },
                new MenuItem { Id = "broodje_ham_kaas", Name = "Broodje Ham-Kaas" },
                new MenuItem { Id = "broodje_salada", Name = "Broodje Salada" },
                new MenuItem { Id = "broodje_gezond", Name = "Broodje Gezond" },
                new MenuItem { Id = "broodje_shoarma", Name = "Broodje Shoarma" },
                new MenuItem { Id = "broodje_viandel", Name = "Broodje Viandel" },
                new MenuItem { Id = "broodje_frikandel", Name = "Broodje Frikandel" },
                new MenuItem { Id = "broodje_kroket", Name = "Broodje Kroket" }
            ],
            VeggieSnacks =
            [
                new MenuItem { Id = "veggie_kaassouffle", Name = "Kaassoufflé" },
                new MenuItem { Id = "veggie_kroket_kaas", Name = "Kaaskroket" },
                new MenuItem { Id = "veggie_kroket_groente", Name = "Groentekroket" },
                new MenuItem { Id = "veggie_cheese_crack", Name = "Cheese Crack" },
                new MenuItem { Id = "veggie_kroket_asperge", Name = "Aspergekroket" },
                new MenuItem { Id = "veggie_gehaktbal", Name = "Bonita" },
                new MenuItem { Id = "veggie_loempia", Name = "Loempia" },
                new MenuItem { Id = "veggie_visstick", Name = "Visstick" },
                new MenuItem { Id = "veggie_kroket_garnalen", Name = "Garnalenkroket" }
            ],
            SchotelsMetSaladesEnFrites =
            [
                new MenuItem { Id = "schotel_sf_schnitzel_sates", Name = "Schnitzel + satésaus" },
                new MenuItem { Id = "schotel_sf_schnitzel_zigeneur", Name = "Schnitzel + zigeunersaus" },
                new MenuItem { Id = "schotel_sf_schnitzel_champignon", Name = "Schnitzel + champignonsaus" },
                new MenuItem { Id = "schotel_sf_shoarma_klein", Name = "Shoarma klein" },
                new MenuItem { Id = "schotel_sf_shoarma_groot", Name = "Shoarma groot" },
                new MenuItem { Id = "schotel_sf_giros_klein", Name = "Giros klein" },
                new MenuItem { Id = "schotel_sf_giros_groot", Name = "Giros groot" },
                new MenuItem { Id = "schotel_sf_shaslick", Name = "Shaslick" },
                new MenuItem { Id = "schotel_sf_kapsalon", Name = "Kapsalon" },
                new MenuItem { Id = "schotel_sf_loempia", Name = "Loempia" },
                new MenuItem { Id = "schotel_sf_halvehaan", Name = "Halve haan" },
                new MenuItem { Id = "schotel_sf_sate", Name = "Saté (kip / varkenshaas)" },
                new MenuItem { Id = "schotel_sf_goulash", Name = "Goulash" },
                new MenuItem { Id = "schotel_sf_zuurvlees", Name = "Zuurvlees" },
                new MenuItem { Id = "schotel_sf_van_het_huis", Name = "Schotel van het huis" },
                new MenuItem { Id = "schotel_sf_kindermenu", Name = "Kindermenubox + verrassing" }
            ],
            SchotelsMetSaladesZonderFrites =
            [
                new MenuItem { Id = "schotel_s_bami", Name = "Bami (ei + saté)" },
                new MenuItem { Id = "schotel_s_nasi", Name = "Nasi (ei + saté)" },
                new MenuItem { Id = "schotel_s_spaghetti", Name = "Spaghetti" },
                new MenuItem { Id = "schotel_s_macaroni", Name = "Macaroni" },
                new MenuItem { Id = "schotel_s_spaghetti_speciaal", Name = "Spaghetti speciaal" },
                new MenuItem { Id = "schotel_s_macaroni_speciaal", Name = "Macaroni speciaal" }
            ],
            Diversen =
            [
                new MenuItem { Id = "divers_salade_klein", Name = "Portie salade klein" },
                new MenuItem { Id = "divers_salade_groot", Name = "Portie salade groot" },
                new MenuItem { Id = "divers_slaatje", Name = "Slaatje" },
                new MenuItem { Id = "divers_soep_van_de_dag", Name = "Soep van de dag" },
                new MenuItem { Id = "divers_uitsmijter", Name = "Uitsmijter" },
                new MenuItem { Id = "divers_uitsmijter_hawai", Name = "Uitsmijter hawaï" },
                new MenuItem { Id = "divers_uitsmijter_smulhoven", Name = "Uitsmijter smulhoven" }
            ],
            Dranken =
            [
                new MenuItem { Id = "cola", Name = "Cola" },
                new MenuItem { Id = "cola_zero", Name = "Cola Zero" },
                new MenuItem { Id = "cola_halve_liter", Name = "Cola (1/2 liter)" },
                new MenuItem { Id = "fanta", Name = "Fanta" },
                new MenuItem { Id = "sprite", Name = "Sprite" },
                new MenuItem { Id = "spa_groen", Name = "Spa groen (1/2 liter)" },
                new MenuItem { Id = "spa_blauw", Name = "Spa blauw (1/2 liter)" },
                new MenuItem { Id = "spa_rood", Name = "Spa rood (1/2 liter)" },
                new MenuItem { Id = "aa_drink", Name = "AA Drink" },
                new MenuItem { Id = "dr_pepper", Name = "Dr. Pepper" },
                new MenuItem { Id = "aquarius", Name = "Aquarius (1/2 liter)" },
                new MenuItem { Id = "red_bull", Name = "Red Bull" },
                new MenuItem { Id = "lipton", Name = "Liptonice" },
                new MenuItem { Id = "appelsap", Name = "Appelsap" },
                new MenuItem { Id = "chocomel", Name = "Chocomel" },
                new MenuItem { Id = "fristi", Name = "Fristi" },
                new MenuItem { Id = "cassis", Name = "Cassis" },
                new MenuItem { Id = "bier_alcoholvrij", Name = "Bier (alcohol vrij)" },
                new MenuItem { Id = "bier_heineken", Name = "Bier Heineken (blikje)" },
                new MenuItem { Id = "bier_grolsch", Name = "Bier Grolsch (blikje)" },
                new MenuItem { Id = "bier_halve_liter", Name = "Bier (1/2 liter - blikje)" }
            ],
            WarmeDranken =
            [
                new MenuItem { Id = "koffie", Name = "Koffie" },
                new MenuItem { Id = "thee", Name = "Thee" },
                new MenuItem { Id = "chocomel_warm", Name = "Chocomel" },
                new MenuItem { Id = "chocomel_warm_slagroom", Name = "Chocomel met slagroom" }
            ],
            Extras =
            [
                new MenuItem { Id = "bakje_mayo", Name = "Losse Mayo" },
                new MenuItem { Id = "bakje_mayo_belgisch", Name = "Losse Belgische Mayo" },
                new MenuItem { Id = "bakje_curry", Name = "Losse Curry" },
                new MenuItem { Id = "bakje_ketchup", Name = "Losse Ketchup" },
                new MenuItem { Id = "bakje_pindasaus", Name = "Losse Pindasaus" },
                new MenuItem { Id = "bakje_appelmoes", Name = "Losse Appelmoes" }
            ]
        };
    }

    public MenuConfig GetMenuConfig() => _menuConfig;

    public MenuItem? GetMenuItem(string type, string id)
    {
        return type.ToLower() switch
        {
            "friet" => _menuConfig.Friet.FirstOrDefault(x => x.Id == id),
            "snacks" => _menuConfig.Snacks.FirstOrDefault(x => x.Id == id),
            "burgers" => _menuConfig.Burgers.FirstOrDefault(x => x.Id == id),
            "broodjes" => _menuConfig.Broodjes.FirstOrDefault(x => x.Id == id),
            "veggie_snacks" => _menuConfig.VeggieSnacks.FirstOrDefault(x => x.Id == id),
            "schotels_met_salades_en_frites" => _menuConfig.SchotelsMetSaladesEnFrites.FirstOrDefault(x => x.Id == id),
            "schotels_met_salades_zonder_frites" => _menuConfig.SchotelsMetSaladesZonderFrites.FirstOrDefault(x => x.Id == id),
            "diversen" => _menuConfig.Diversen.FirstOrDefault(x => x.Id == id),
            "dranken" => _menuConfig.Dranken.FirstOrDefault(x => x.Id == id),
            "warme_dranken" => _menuConfig.WarmeDranken.FirstOrDefault(x => x.Id == id),
            "extras" => _menuConfig.Extras.FirstOrDefault(x => x.Id == id),
            _ => null
        };
    }
} 