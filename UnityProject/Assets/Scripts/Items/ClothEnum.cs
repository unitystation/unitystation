using System.ComponentModel;

public enum ClothEnum
{
	[Description("")] none,

	[Description("/obj/item/clothing/ears/earmuffs")] earmuffs__clothing_ears_earmuffs,

	[Description("/obj/item/clothing/glasses/chameleon")] Optical_Meson_Scanner__clothing_glasses_chameleon,

	[Description("/obj/item/clothing/glasses/cold")] cold_goggles__clothing_glasses_cold,

	[Description("/obj/item/clothing/glasses/eyepatch")] eyepatch__clothing_glasses_eyepatch,

	[Description("/obj/item/clothing/glasses/godeye")] eye_of_god__clothing_glasses_godeye,

	[Description("/obj/item/clothing/glasses/heat")] heat_goggles__clothing_glasses_heat,

	[Description("/obj/item/clothing/glasses/hud/diagnostic")] Diagnostic_HUD__glasses_hud_diagnostic,

	[Description("/obj/item/clothing/glasses/hud/diagnostic/night")] Night_Vision_Diagnostic_HUD__hud_diagnostic_night,

	[Description("/obj/item/clothing/glasses/hud/health")] Health_Scanner_HUD__glasses_hud_health,

	[Description("/obj/item/clothing/glasses/hud/health/night")] Night_Vision_Health_Scanner_HUD__hud_health_night,

	[Description("/obj/item/clothing/glasses/hud/health/sunglasses")] Medical_HUDSunglasses__hud_health_sunglasses,

	[Description("/obj/item/clothing/glasses/hud/security")] Security_HUD__glasses_hud_security,

	[Description("/obj/item/clothing/glasses/hud/security/night")] Night_Vision_Security_HUD__hud_security_night,

	[Description("/obj/item/clothing/glasses/hud/security/sunglasses")] Security_HUDSunglasses__hud_security_sunglasses,

	[Description("/obj/item/clothing/glasses/hud/security/sunglasses/eyepatch")] Eyepatch_HUD__security_sunglasses_eyepatch,

	[Description("/obj/item/clothing/glasses/hud/security/sunglasses/gars")] HUD_gar_glasses__security_sunglasses_gars,

	[Description("/obj/item/clothing/glasses/hud/security/sunglasses/gars/supergars")] giga_HUD_gar_glasses__sunglasses_gars_supergars,

	[Description("/obj/item/clothing/glasses/hud/toggle/thermal")] Thermal_HUD_Scanner__hud_toggle_thermal,

	[Description("/obj/item/clothing/glasses/judicial_visor")] judicial_visor__clothing_glasses_judicial_visor,

	[Description("/obj/item/clothing/glasses/material")] Optical_Material_Scanner__clothing_glasses_material,

	[Description("/obj/item/clothing/glasses/material/mining")] Optical_Material_Scanner__glasses_material_mining,

	[Description("/obj/item/clothing/glasses/material/mining/gar")] gar_material_scanner__material_mining_gar,

	[Description("/obj/item/clothing/glasses/meson")] Optical_Meson_Scanner__clothing_glasses_meson,

	[Description("/obj/item/clothing/glasses/meson/engine")] Engineering_Scanner_Goggles__glasses_meson_engine,

	[Description("/obj/item/clothing/glasses/meson/engine/tray")] Optical_T_Ray_Scanner__meson_engine_tray,

	[Description("/obj/item/clothing/glasses/meson/gar")] gar_mesons__glasses_meson_gar,

	[Description("/obj/item/clothing/glasses/meson/night")] Night_Vision_Optical_Meson_Scanner__glasses_meson_night,

	[Description("/obj/item/clothing/glasses/meson/truesight")] The_Lens_of_Truesight__glasses_meson_truesight,

	[Description("/obj/item/clothing/glasses/monocle")] monocle__clothing_glasses_monocle,

	[Description("/obj/item/clothing/glasses/night")] Night_Vision_Goggles__clothing_glasses_night,

	[Description("/obj/item/clothing/glasses/night/cultblind")] zealots_blindfold__glasses_night_cultblind,

	[Description("/obj/item/clothing/glasses/orange")] orange_glasses__clothing_glasses_orange,

	[Description("/obj/item/clothing/glasses/red")] red_glasses__clothing_glasses_red,

	[Description("/obj/item/clothing/glasses/regular")] Prescription_Glasses__clothing_glasses_regular,

	[Description("/obj/item/clothing/glasses/regular/hipster")] Prescription_Glasses__glasses_regular_hipster,

	[Description("/obj/item/clothing/glasses/regular/jamjar")] Jamjar_Glasses__glasses_regular_jamjar,

	[Description("/obj/item/clothing/glasses/science")] science_goggles__clothing_glasses_science,

	[Description("/obj/item/clothing/glasses/sunglasses")] sunglasses__clothing_glasses_sunglasses,

	[Description("/obj/item/clothing/glasses/sunglasses/blindfold")] blindfold__glasses_sunglasses_blindfold,

	[Description("/obj/item/clothing/glasses/sunglasses/gar")] gar_glasses__glasses_sunglasses_gar,

	[Description("/obj/item/clothing/glasses/sunglasses/gar/supergar")] giga_gar_glasses__sunglasses_gar_supergar,

	[Description("/obj/item/clothing/glasses/sunglasses/garb")] black_gar_glasses__glasses_sunglasses_garb,

	[Description("/obj/item/clothing/glasses/sunglasses/garb/supergarb")] black_giga_gar_glasses__sunglasses_garb_supergarb,

	[Description("/obj/item/clothing/glasses/thermal")] Optical_Thermal_Scanner__clothing_glasses_thermal,

	[Description("/obj/item/clothing/glasses/thermal/eyepatch")] Optical_Thermal_Eyepatch__glasses_thermal_eyepatch,

	[Description("/obj/item/clothing/glasses/thermal/monocle")] Thermoncle__glasses_thermal_monocle,

	[Description("/obj/item/clothing/glasses/welding")] welding_goggles__clothing_glasses_welding,

	[Description("/obj/item/clothing/glasses/wraith_spectacles")] antique_spectacles__clothing_glasses_wraith_spectacles,

	[Description("/obj/item/clothing/gloves/botanic_leather")] botanists_leather_gloves__clothing_gloves_botanic_leather,

	[Description("/obj/item/clothing/gloves/boxing")] boxing_gloves__clothing_gloves_boxing,

	[Description("/obj/item/clothing/gloves/bracer")] bone_bracers__clothing_gloves_bracer,

	[Description("/obj/item/clothing/gloves/chameleon")] insulated_gloves__clothing_gloves_chameleon,

	[Description("/obj/item/clothing/gloves/clockwork")] clockwork_gauntlets__clothing_gloves_clockwork,

	[Description("/obj/item/clothing/gloves/color/black")] black_gloves__gloves_color_black,

	[Description("/obj/item/clothing/gloves/color/blue")] blue_gloves__gloves_color_blue,

	[Description("/obj/item/clothing/gloves/color/brown")] brown_gloves__gloves_color_brown,

	[Description("/obj/item/clothing/gloves/color/captain")] captains_gloves__gloves_color_captain,

	[Description("/obj/item/clothing/gloves/color/fyellow")] budget_insulated_gloves__gloves_color_fyellow,

	[Description("/obj/item/clothing/gloves/color/green")] green_gloves__gloves_color_green,

	[Description("/obj/item/clothing/gloves/color/grey")] grey_gloves__gloves_color_grey,

	[Description("/obj/item/clothing/gloves/color/latex")] latex_gloves__gloves_color_latex,

	[Description("/obj/item/clothing/gloves/color/latex/nitrile")] nitrile_gloves__color_latex_nitrile,

	[Description("/obj/item/clothing/gloves/color/light_brown")] light_brown_gloves__gloves_color_light_brown,

	[Description("/obj/item/clothing/gloves/color/orange")] orange_gloves__gloves_color_orange,

	[Description("/obj/item/clothing/gloves/color/purple")] purple_gloves__gloves_color_purple,

	[Description("/obj/item/clothing/gloves/color/rainbow")] rainbow_gloves__gloves_color_rainbow,

	[Description("/obj/item/clothing/gloves/color/random")] random_gloves__gloves_color_random,

	[Description("/obj/item/clothing/gloves/color/red")] red_gloves__gloves_color_red,

	[Description("/obj/item/clothing/gloves/color/white")] white_gloves__gloves_color_white,

	[Description("/obj/item/clothing/gloves/color/yellow")] insulated_gloves__gloves_color_yellow,

	[Description("/obj/item/clothing/gloves/combat")] combat_gloves__clothing_gloves_combat,

	[Description("/obj/item/clothing/gloves/fingerless")] fingerless_gloves__clothing_gloves_fingerless,

	[Description("/obj/item/clothing/gloves/gang")] Badass_Belt__clothing_gloves_gang,

	[Description("/obj/item/clothing/gloves/golem")] golems_hands__clothing_gloves_golem,

	[Description("/obj/item/clothing/gloves/krav_maga/sec")] krav_maga_gloves__gloves_krav_maga_sec,

	[Description("/obj/item/clothing/gloves/plate")] Plate_Gauntlets__clothing_gloves_plate,

	[Description("/obj/item/clothing/gloves/space_ninja")] ninja_gloves__clothing_gloves_space_ninja,

	[Description("/obj/item/clothing/head")] head__item_clothing_head,

	[Description("/obj/item/clothing/head/HoS")] head_of_security_cap__clothing_head_HoS,

	[Description("/obj/item/clothing/head/HoS/beret")] head_of_security_beret__head_HoS_beret,

	[Description("/obj/item/clothing/head/bandana")] pirate_bandana__clothing_head_bandana,

	[Description("/obj/item/clothing/head/beanie")] white_beanie__clothing_head_beanie,

	[Description("/obj/item/clothing/head/beanie/black")] black_beanie__head_beanie_black,

	[Description("/obj/item/clothing/head/beanie/christmas")] christmas_beanie__head_beanie_christmas,

	[Description("/obj/item/clothing/head/beanie/cyan")] cyan_beanie__head_beanie_cyan,

	[Description("/obj/item/clothing/head/beanie/darkblue")] dark_blue_beanie__head_beanie_darkblue,

	[Description("/obj/item/clothing/head/beanie/green")] green_beanie__head_beanie_green,

	[Description("/obj/item/clothing/head/beanie/orange")] orange_beanie__head_beanie_orange,

	[Description("/obj/item/clothing/head/beanie/purple")] purple_beanie__head_beanie_purple,

	[Description("/obj/item/clothing/head/beanie/red")] red_beanie__head_beanie_red,

	[Description("/obj/item/clothing/head/beanie/striped")] striped_beanie__head_beanie_striped,

	[Description("/obj/item/clothing/head/beanie/stripedblue")] blue_striped_beanie__head_beanie_stripedblue,

	[Description("/obj/item/clothing/head/beanie/stripedgreen")] green_striped_beanie__head_beanie_stripedgreen,

	[Description("/obj/item/clothing/head/beanie/stripedred")] red_striped_beanie__head_beanie_stripedred,

	[Description("/obj/item/clothing/head/beanie/yellow")] yellow_beanie__head_beanie_yellow,

	[Description("/obj/item/clothing/head/bearpelt")] bear_pelt_hat__clothing_head_bearpelt,

	[Description("/obj/item/clothing/head/beekeeper_head")] beekeeper_hat__clothing_head_beekeeper_head,

	[Description("/obj/item/clothing/head/beret")] beret__clothing_head_beret,

	[Description("/obj/item/clothing/head/beret/black")] black_beret__head_beret_black,

	[Description("/obj/item/clothing/head/beret/sec")] security_beret__head_beret_sec,

	[Description("/obj/item/clothing/head/beret/sec/navyhos")] head_of_securitys_beret__beret_sec_navyhos,

	[Description("/obj/item/clothing/head/beret/sec/navywarden")] wardens_beret__beret_sec_navywarden,

	[Description("/obj/item/clothing/head/bio_hood")] bio_hood__clothing_head_bio_hood,

	[Description("/obj/item/clothing/head/bomb_hood")] bomb_hood__clothing_head_bomb_hood,

	[Description("/obj/item/clothing/head/bowler")] bowler_hat__clothing_head_bowler,

	[Description("/obj/item/clothing/head/bunnyhead")] Easter_Bunny_Head__clothing_head_bunnyhead,

	[Description("/obj/item/clothing/head/cage")] cage__clothing_head_cage,

	[Description("/obj/item/clothing/head/canada")] striped_red_tophat__clothing_head_canada,

	[Description("/obj/item/clothing/head/caphat")] captains_hat__clothing_head_caphat,

	[Description("/obj/item/clothing/head/caphat/parade")] captains_parade_cap__head_caphat_parade,

	[Description("/obj/item/clothing/head/cardborg")] cardborg_helmet__clothing_head_cardborg,

	[Description("/obj/item/clothing/head/centhat")] improper_Centcom_hat__clothing_head_centhat,

	[Description("/obj/item/clothing/head/chameleon")] grey_cap__clothing_head_chameleon,

	[Description("/obj/item/clothing/head/chefhat")] chefs_hat__clothing_head_chefhat,

	[Description("/obj/item/clothing/head/chicken")] chicken_suit_head__clothing_head_chicken,

	[Description("/obj/item/clothing/head/cloakhood")] cloak_hood__clothing_head_cloakhood,

	[Description("/obj/item/clothing/head/collectable/HoP")] collectable_HoP_hat__head_collectable_HoP,

	[Description("/obj/item/clothing/head/collectable/HoS")] collectable_HoS_hat__head_collectable_HoS,

	[Description("/obj/item/clothing/head/collectable/beret")] collectable_beret__head_collectable_beret,

	[Description("/obj/item/clothing/head/collectable/captain")] collectable_captains_hat__head_collectable_captain,

	[Description("/obj/item/clothing/head/collectable/chef")] collectable_chefs_hat__head_collectable_chef,

	[Description("/obj/item/clothing/head/collectable/flatcap")] collectable_flat_cap__head_collectable_flatcap,

	[Description("/obj/item/clothing/head/collectable/hardhat")] collectable_hard_hat__head_collectable_hardhat,

	[Description("/obj/item/clothing/head/collectable/kitty")] collectable_kitty_ears__head_collectable_kitty,

	[Description("/obj/item/clothing/head/collectable/paper")] collectable_paper_hat__head_collectable_paper,

	[Description("/obj/item/clothing/head/collectable/petehat")] ultra_rare_Petes_hat__head_collectable_petehat,

	[Description("/obj/item/clothing/head/collectable/pirate")] collectable_pirate_hat__head_collectable_pirate,

	[Description("/obj/item/clothing/head/collectable/police")] collectable_police_officers_hat__head_collectable_police,

	[Description("/obj/item/clothing/head/collectable/rabbitears")] collectable_rabbit_ears__head_collectable_rabbitears,

	[Description("/obj/item/clothing/head/collectable/slime")] collectable_slime_cap__head_collectable_slime,

	[Description("/obj/item/clothing/head/collectable/swat")] collectable_SWAT_helmet__head_collectable_swat,

	[Description("/obj/item/clothing/head/collectable/thunderdome")] collectable_Thunderdome_helmet__head_collectable_thunderdome,

	[Description("/obj/item/clothing/head/collectable/tophat")] collectable_top_hat__head_collectable_tophat,

	[Description("/obj/item/clothing/head/collectable/welding")] collectable_welding_helmet__head_collectable_welding,

	[Description("/obj/item/clothing/head/collectable/wizard")] collectable_wizards_hat__head_collectable_wizard,

	[Description("/obj/item/clothing/head/collectable/xenom")] collectable_xenomorph_helmet__head_collectable_xenom,

	[Description("/obj/item/clothing/head/cone")] warning_cone__clothing_head_cone,

	[Description("/obj/item/clothing/head/crown")] crown__clothing_head_crown,

	[Description("/obj/item/clothing/head/crown/fancy")] magnificent_crown__head_crown_fancy,

	[Description("/obj/item/clothing/head/cueball")] cueball_helmet__clothing_head_cueball,

	[Description("/obj/item/clothing/head/culthood")] ancient_cultist_hood__clothing_head_culthood,

	[Description("/obj/item/clothing/head/culthood/alt")] cultist_hood__head_culthood_alt,

	[Description("/obj/item/clothing/head/curator")] treasure_hunters_fedora__clothing_head_curator,

	[Description("/obj/item/clothing/head/det_hat")] detectives_fedora__clothing_head_det_hat,

	[Description("/obj/item/clothing/head/drone_holder")] drone_hiding__clothing_head_drone_holder,

	[Description("/obj/item/clothing/head/fedora")] fedora__clothing_head_fedora,

	[Description("/obj/item/clothing/head/festive")] festive_paper_hat__clothing_head_festive,

	[Description("/obj/item/clothing/head/flatcap")] flat_cap__clothing_head_flatcap,

	[Description("/obj/item/clothing/head/griffin")] griffon_head__clothing_head_griffin,

	[Description("/obj/item/clothing/head/hardhat")] hard_hat__clothing_head_hardhat,

	[Description("/obj/item/clothing/head/hardhat/atmos")] atmospheric_technicians_firefighting_helmet__head_hardhat_atmos,

	[Description("/obj/item/clothing/head/hardhat/cakehat")] cakehat__head_hardhat_cakehat,

	[Description("/obj/item/clothing/head/hardhat/pumpkinhead")] carved_pumpkin__head_hardhat_pumpkinhead,

	[Description("/obj/item/clothing/head/hardhat/red")] firefighter_helmet__head_hardhat_red,

	[Description("/obj/item/clothing/head/hardhat/reindeer")] novelty_reindeer_hat__head_hardhat_reindeer,

	[Description("/obj/item/clothing/head/hasturhood")] hasturs_hood__clothing_head_hasturhood,

	[Description("/obj/item/clothing/head/helmet")] helmet__clothing_head_helmet,

	[Description("/obj/item/clothing/head/helmet/abductor")] agent_headgear__head_helmet_abductor,

	[Description("/obj/item/clothing/head/helmet/alt")] bulletproof_helmet__head_helmet_alt,

	[Description("/obj/item/clothing/head/helmet/bluetaghelm")] blue_laser_tag_helmet__head_helmet_bluetaghelm,

	[Description("/obj/item/clothing/head/helmet/changeling")] chitinous_mass__head_helmet_changeling,

	[Description("/obj/item/clothing/head/helmet/clockwork")] clockwork_helmet__head_helmet_clockwork,

	[Description("/obj/item/clothing/head/helmet/gladiator")] gladiator_helmet__head_helmet_gladiator,

	[Description("/obj/item/clothing/head/helmet/justice")] helmet_of_justice__head_helmet_justice,

	[Description("/obj/item/clothing/head/helmet/justice/escape")] alarm_helmet__helmet_justice_escape,

	[Description("/obj/item/clothing/head/helmet/knight")] medieval_helmet__head_helmet_knight,

	[Description("/obj/item/clothing/head/helmet/knight/templar")] crusader_helmet__helmet_knight_templar,

	[Description("/obj/item/clothing/head/helmet/plate/crusader")] Crusaders_Hood__helmet_plate_crusader,

	[Description("/obj/item/clothing/head/helmet/redtaghelm")] red_laser_tag_helmet__head_helmet_redtaghelm,

	[Description("/obj/item/clothing/head/helmet/riot")] riot_helmet__head_helmet_riot,

	[Description("/obj/item/clothing/head/helmet/roman")] roman_helmet__head_helmet_roman,

	[Description("/obj/item/clothing/head/helmet/roman/legionaire")] roman_legionaire_helmet__helmet_roman_legionaire,

	[Description("/obj/item/clothing/head/helmet/skull")] skull_helmet__head_helmet_skull,

	[Description("/obj/item/clothing/head/helmet/space")] space_helmet__head_helmet_space,

	[Description("/obj/item/clothing/head/helmet/space/beret")] officers_beret__helmet_space_beret,

	[Description("/obj/item/clothing/head/helmet/space/changeling")] flesh_mass__helmet_space_changeling,

	[Description("/obj/item/clothing/head/helmet/space/chronos")] Chronosuit_Helmet__helmet_space_chronos,

	[Description("/obj/item/clothing/head/helmet/space/eva")] EVA_helmet__helmet_space_eva,

	[Description("/obj/item/clothing/head/helmet/space/fragile")] emergency_space_helmet__helmet_space_fragile,

	[Description("/obj/item/clothing/head/helmet/space/freedom")] eagle_helmet__helmet_space_freedom,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit")] hardsuit_helmet__helmet_space_hardsuit,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/captain")] captains_hardsuit_helmet__space_hardsuit_captain,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/carp")] carp_helmet__space_hardsuit_carp,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/clown")] cosmohonk_hardsuit_helmet__space_hardsuit_clown,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/cult")] nar_sien_hardened_helmet__space_hardsuit_cult,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/deathsquad")] MKIII_SWAT_Helmet__space_hardsuit_deathsquad,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/engine")] engineering_hardsuit_helmet__space_hardsuit_engine,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/engine/atmos")] atmospherics_hardsuit_helmet__hardsuit_engine_atmos,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/engine/elite")] advanced_hardsuit_helmet__hardsuit_engine_elite,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/ert")] emergency_response_unit_helmet__space_hardsuit_ert,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/ert/paranormal")] paranormal_response_unit_helmet__hardsuit_ert_paranormal,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/ert/paranormal/beserker")] champions_helmet__ert_paranormal_beserker,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/ert/paranormal/inquisitor")] inquisitors_helmet__ert_paranormal_inquisitor,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/flightsuit")] flight_helmet__space_hardsuit_flightsuit,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/medical")] medical_hardsuit_helmet__space_hardsuit_medical,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/mining")] mining_hardsuit_helmet__space_hardsuit_mining,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/rd")] prototype_hardsuit_helmet__space_hardsuit_rd,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/security")] security_hardsuit_helmet__space_hardsuit_security,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/security/hos")] head_of_securitys_hardsuit_helmet__hardsuit_security_hos,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/shielded/ctf")] shielded_hardsuit_helmet__hardsuit_shielded_ctf,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/shielded/ctf/blue")] shielded_hardsuit_helmet__shielded_ctf_blue,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/shielded/swat")] death_commando_helmet__hardsuit_shielded_swat,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/shielded/syndi")] blood_red_hardsuit_helmet__hardsuit_shielded_syndi,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/shielded/wizard")] battlemage_helmet__hardsuit_shielded_wizard,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/syndi")] blood_red_hardsuit_helmet__space_hardsuit_syndi,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/syndi/elite")] elite_syndicate_hardsuit_helmet__hardsuit_syndi_elite,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/syndi/owl")] owl_hardsuit_helmet__hardsuit_syndi_owl,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/ueg")] Iron_Hawk_Marine_Helmet__space_hardsuit_ueg,

	[Description("/obj/item/clothing/head/helmet/space/hardsuit/wizard")] gem_encrusted_hardsuit_helmet__space_hardsuit_wizard,

	[Description("/obj/item/clothing/head/helmet/space/nasavoid")] NASA_Void_Helmet__helmet_space_nasavoid,

	[Description("/obj/item/clothing/head/helmet/space/orange")] emergency_space_helmet__helmet_space_orange,

	[Description("/obj/item/clothing/head/helmet/space/pirate")] pirate_hat__helmet_space_pirate,

	[Description("/obj/item/clothing/head/helmet/space/plasmaman")] plasma_envirosuit_helmet__helmet_space_plasmaman,

	[Description("/obj/item/clothing/head/helmet/space/santahat")] Santas_hat__helmet_space_santahat,

	[Description("/obj/item/clothing/head/helmet/space/space_ninja")] ninja_hood__helmet_space_space_ninja,

	[Description("/obj/item/clothing/head/helmet/space/syndicate")] red_space_helmet__helmet_space_syndicate,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/black")] black_space_helmet__space_syndicate_black,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/black/blue")] black_space_helmet__syndicate_black_blue,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/black/engie")] black_space_helmet__syndicate_black_engie,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/black/green")] black_space_helmet__syndicate_black_green,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/black/med")] black_space_helmet__syndicate_black_med,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/black/orange")] black_space_helmet__syndicate_black_orange,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/black/red")] black_space_helmet__syndicate_black_red,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/blue")] blue_space_helmet__space_syndicate_blue,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/green")] green_space_helmet__space_syndicate_green,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/green/dark")] dark_green_space_helmet__syndicate_green_dark,

	[Description("/obj/item/clothing/head/helmet/space/syndicate/orange")] orange_space_helmet__space_syndicate_orange,

	[Description("/obj/item/clothing/head/helmet/swat")] improper_SWAT_helmet__head_helmet_swat,

	[Description("/obj/item/clothing/head/helmet/swat/nanotrasen")] improper_SWAT_helmet__helmet_swat_nanotrasen,

	[Description("/obj/item/clothing/head/helmet/thunderdome")] improper_Thunderdome_helmet__head_helmet_thunderdome,

	[Description("/obj/item/clothing/head/hooded/bee_hood")] bee_hood__head_hooded_bee_hood,

	[Description("/obj/item/clothing/head/hooded/berserkerhood")] flagellants_robes__head_hooded_berserkerhood,

	[Description("/obj/item/clothing/head/hooded/carp_hood")] carp_hood__head_hooded_carp_hood,

	[Description("/obj/item/clothing/head/hooded/chaplain_hood")] chaplain_hood__head_hooded_chaplain_hood,

	[Description("/obj/item/clothing/head/hooded/cloakhood/drake")] drake_helm__hooded_cloakhood_drake,

	[Description("/obj/item/clothing/head/hooded/cloakhood/goliath")] goliath_cloak_hood__hooded_cloakhood_goliath,

	[Description("/obj/item/clothing/head/hooded/cult_hoodie")] empowered_cultist_armor__head_hooded_cult_hoodie,

	[Description("/obj/item/clothing/head/hooded/explorer")] explorer_hood__head_hooded_explorer,

	[Description("/obj/item/clothing/head/hooded/human_head")] bloated_human_head__head_hooded_human_head,

	[Description("/obj/item/clothing/head/hooded/ian_hood")] corgi_hood__head_hooded_ian_hood,

	[Description("/obj/item/clothing/head/hooded/winterhood")] winter_hood__head_hooded_winterhood,

	[Description("/obj/item/clothing/head/hopcap")] head_of_personnels_cap__clothing_head_hopcap,

	[Description("/obj/item/clothing/head/jester")] jester_hat__clothing_head_jester,

	[Description("/obj/item/clothing/head/justice")] justice_hat__clothing_head_justice,

	[Description("/obj/item/clothing/head/kitty")] kitty_ears__clothing_head_kitty,

	[Description("/obj/item/clothing/head/lizard")] lizardskin_cloche_hat__clothing_head_lizard,

	[Description("/obj/item/clothing/head/magus")] magus_helm__clothing_head_magus,

	[Description("/obj/item/clothing/head/mailman")] mailmans_hat__clothing_head_mailman,

	[Description("/obj/item/clothing/head/nun_hood")] nun_hood__clothing_head_nun_hood,

	[Description("/obj/item/clothing/head/nursehat")] nurses_hat__clothing_head_nursehat,

	[Description("/obj/item/clothing/head/papersack")] paper_sack_hat__clothing_head_papersack,

	[Description("/obj/item/clothing/head/papersack/smiley")] paper_sack_hat__head_papersack_smiley,

	[Description("/obj/item/clothing/head/pirate")] pirate_hat__clothing_head_pirate,

	[Description("/obj/item/clothing/head/plaguedoctorhat")] plague_doctors_hat__clothing_head_plaguedoctorhat,

	[Description("/obj/item/clothing/head/powdered_wig")] powdered_wig__clothing_head_powdered_wig,

	[Description("/obj/item/clothing/head/rabbitears")] rabbit_ears__clothing_head_rabbitears,

	[Description("/obj/item/clothing/head/radiation")] radiation_hood__clothing_head_radiation,

	[Description("/obj/item/clothing/head/redcoat")] redcoats_hat__clothing_head_redcoat,

	[Description("/obj/item/clothing/head/rice_hat")] rice_hat__clothing_head_rice_hat,

	[Description("/obj/item/clothing/head/santa")] santa_hat__clothing_head_santa,

	[Description("/obj/item/clothing/head/snowman")] Snowman_Head__clothing_head_snowman,

	[Description("/obj/item/clothing/head/soft")] cargo_cap__clothing_head_soft,

	[Description("/obj/item/clothing/head/soft/black")] black_cap__head_soft_black,

	[Description("/obj/item/clothing/head/soft/blue")] blue_cap__head_soft_blue,

	[Description("/obj/item/clothing/head/soft/emt")] EMT_cap__head_soft_emt,

	[Description("/obj/item/clothing/head/soft/green")] green_cap__head_soft_green,

	[Description("/obj/item/clothing/head/soft/grey")] grey_cap__head_soft_grey,

	[Description("/obj/item/clothing/head/soft/mime")] white_cap__head_soft_mime,

	[Description("/obj/item/clothing/head/soft/orange")] orange_cap__head_soft_orange,

	[Description("/obj/item/clothing/head/soft/purple")] purple_cap__head_soft_purple,

	[Description("/obj/item/clothing/head/soft/rainbow")] rainbow_cap__head_soft_rainbow,

	[Description("/obj/item/clothing/head/soft/red")] red_cap__head_soft_red,

	[Description("/obj/item/clothing/head/soft/sec")] security_cap__head_soft_sec,

	[Description("/obj/item/clothing/head/soft/yellow")] yellow_cap__head_soft_yellow,

	[Description("/obj/item/clothing/head/sombrero")] sombrero__clothing_head_sombrero,

	[Description("/obj/item/clothing/head/sombrero/green")] green_sombrero__head_sombrero_green,

	[Description("/obj/item/clothing/head/sombrero/shamebrero")] shamebrero__head_sombrero_shamebrero,

	[Description("/obj/item/clothing/head/space/golem")] golems_head__head_space_golem,

	[Description("/obj/item/clothing/head/syndicatefake")] black_space_helmet_replica__clothing_head_syndicatefake,

	[Description("/obj/item/clothing/head/that")] top_hat__clothing_head_that,

	[Description("/obj/item/clothing/head/ushanka")] ushanka__clothing_head_ushanka,

	[Description("/obj/item/clothing/head/warden")] wardens_police_hat__clothing_head_warden,

	[Description("/obj/item/clothing/head/welding")] welding_helmet__clothing_head_welding,

	[Description("/obj/item/clothing/head/witchunter_hat")] witchunter_hat__clothing_head_witchunter_hat,

	[Description("/obj/item/clothing/head/witchwig")] witch_costume_wig__clothing_head_witchwig,

	[Description("/obj/item/clothing/head/wizard")] wizard_hat__clothing_head_wizard,

	[Description("/obj/item/clothing/head/wizard/black")] black_wizard_hat__head_wizard_black,

	[Description("/obj/item/clothing/head/wizard/fake")] wizard_hat__head_wizard_fake,

	[Description("/obj/item/clothing/head/wizard/magus")] improper_Magus_helm__head_wizard_magus,

	[Description("/obj/item/clothing/head/wizard/marisa")] witch_hat__head_wizard_marisa,

	[Description("/obj/item/clothing/head/wizard/marisa/fake")] witch_hat__wizard_marisa_fake,

	[Description("/obj/item/clothing/head/wizard/red")] red_wizard_hat__head_wizard_red,

	[Description("/obj/item/clothing/head/wizard/santa")] Santas_hat__head_wizard_santa,

	[Description("/obj/item/clothing/head/wizard/yellow")] yellow_wizard_hat__head_wizard_yellow,

	[Description("/obj/item/clothing/head/xenos")] xenos_helmet__clothing_head_xenos,

	[Description("/obj/item/clothing/mask/balaclava")] balaclava__clothing_mask_balaclava,

	[Description("/obj/item/clothing/mask/bandana")] botany_bandana__clothing_mask_bandana,

	[Description("/obj/item/clothing/mask/bandana/black")] black_bandana__mask_bandana_black,

	[Description("/obj/item/clothing/mask/bandana/blue")] blue_bandana__mask_bandana_blue,

	[Description("/obj/item/clothing/mask/bandana/gold")] gold_bandana__mask_bandana_gold,

	[Description("/obj/item/clothing/mask/bandana/green")] green_bandana__mask_bandana_green,

	[Description("/obj/item/clothing/mask/bandana/red")] red_bandana__mask_bandana_red,

	[Description("/obj/item/clothing/mask/bandana/skull")] skull_bandana__mask_bandana_skull,

	[Description("/obj/item/clothing/mask/breath")] breath_mask__clothing_mask_breath,

	[Description("/obj/item/clothing/mask/breath/golem")] golems_face__mask_breath_golem,

	[Description("/obj/item/clothing/mask/breath/medical")] medical_mask__mask_breath_medical,

	[Description("/obj/item/clothing/mask/chameleon")] gas_mask__clothing_mask_chameleon,

	[Description("/obj/item/clothing/mask/cigarette")] cigarette__clothing_mask_cigarette,

	[Description("/obj/item/clothing/mask/cigarette/cigar")] premium_cigar__mask_cigarette_cigar,

	[Description("/obj/item/clothing/mask/cigarette/cigar/cohiba")] improper_Cohiba_Robusto_cigar__cigarette_cigar_cohiba,

	[Description("/obj/item/clothing/mask/cigarette/cigar/havana")] premium_Havanian_cigar__cigarette_cigar_havana,

	[Description("/obj/item/clothing/mask/cigarette/pipe")] smoking_pipe__mask_cigarette_pipe,

	[Description("/obj/item/clothing/mask/cigarette/pipe/cobpipe")] corn_cob_pipe__cigarette_pipe_cobpipe,

	[Description("/obj/item/clothing/mask/cigarette/rollie")] rollie__mask_cigarette_rollie,

	[Description("/obj/item/clothing/mask/cowmask")] Cowface__clothing_mask_cowmask,

	[Description("/obj/item/clothing/mask/facehugger")] alien__clothing_mask_facehugger,

	[Description("/obj/item/clothing/mask/fakemoustache")] fake_moustache__clothing_mask_fakemoustache,

	[Description("/obj/item/clothing/mask/gas")] gas_mask__clothing_mask_gas,

	[Description("/obj/item/clothing/mask/gas/carp")] carp_mask__mask_gas_carp,

	[Description("/obj/item/clothing/mask/gas/clown_hat")] clown_wig_and_mask__mask_gas_clown_hat,

	[Description("/obj/item/clothing/mask/gas/cyborg")] cyborg_visor__mask_gas_cyborg,

	[Description("/obj/item/clothing/mask/gas/death_commando")] Death_Commando_Mask__mask_gas_death_commando,

	[Description("/obj/item/clothing/mask/gas/explorer")] explorer_gas_mask__mask_gas_explorer,

	[Description("/obj/item/clothing/mask/gas/mime")] mime_mask__mask_gas_mime,

	[Description("/obj/item/clothing/mask/gas/monkeymask")] monkey_mask__mask_gas_monkeymask,

	[Description("/obj/item/clothing/mask/gas/owl_mask")] owl_mask__mask_gas_owl_mask,

	[Description("/obj/item/clothing/mask/gas/plaguedoctor")] plague_doctor_mask__mask_gas_plaguedoctor,

	[Description("/obj/item/clothing/mask/gas/sechailer")] security_gas_mask__mask_gas_sechailer,

	[Description("/obj/item/clothing/mask/gas/sechailer/cyborg")] security_hailer__gas_sechailer_cyborg,

	[Description("/obj/item/clothing/mask/gas/sechailer/swat")] improper_SWAT_mask__gas_sechailer_swat,

	[Description("/obj/item/clothing/mask/gas/sexyclown")] sexy_clown_wig_and_mask__mask_gas_sexyclown,

	[Description("/obj/item/clothing/mask/gas/sexymime")] sexy_mime_mask__mask_gas_sexymime,

	[Description("/obj/item/clothing/mask/gas/space_ninja")] ninja_mask__mask_gas_space_ninja,

	[Description("/obj/item/clothing/mask/gas/syndicate")] syndicate_mask__mask_gas_syndicate,

	[Description("/obj/item/clothing/mask/gas/tiki_mask")] tiki_mask__mask_gas_tiki_mask,

	[Description("/obj/item/clothing/mask/gas/welding")] welding_mask__mask_gas_welding,

	[Description("/obj/item/clothing/mask/gskull")] Bling_Boots__clothing_mask_gskull,

	[Description("/obj/item/clothing/mask/horsehead")] horse_head_mask__clothing_mask_horsehead,

	[Description("/obj/item/clothing/mask/joy")] joy_mask__clothing_mask_joy,

	[Description("/obj/item/clothing/mask/luchador")] Luchador_Mask__clothing_mask_luchador,

	[Description("/obj/item/clothing/mask/luchador/rudos")] Rudos_Mask__mask_luchador_rudos,

	[Description("/obj/item/clothing/mask/luchador/tecnicos")] Tecnicos_Mask__mask_luchador_tecnicos,

	[Description("/obj/item/clothing/mask/muzzle")] muzzle__clothing_mask_muzzle,

	[Description("/obj/item/clothing/mask/pig")] pig_mask__clothing_mask_pig,

	[Description("/obj/item/clothing/mask/rat")] rat_mask__clothing_mask_rat,

	[Description("/obj/item/clothing/mask/rat/bat")] bat_mask__mask_rat_bat,

	[Description("/obj/item/clothing/mask/rat/bear")] bear_mask__mask_rat_bear,

	[Description("/obj/item/clothing/mask/rat/bee")] bee_mask__mask_rat_bee,

	[Description("/obj/item/clothing/mask/rat/fox")] fox_mask__mask_rat_fox,

	[Description("/obj/item/clothing/mask/rat/jackal")] jackal_mask__mask_rat_jackal,

	[Description("/obj/item/clothing/mask/rat/raven")] raven_mask__mask_rat_raven,

	[Description("/obj/item/clothing/mask/rat/tribal")] tribal_mask__mask_rat_tribal,

	[Description("/obj/item/clothing/mask/spig")] Pig_face__clothing_mask_spig,

	[Description("/obj/item/clothing/mask/surgical")] sterile_mask__clothing_mask_surgical,

	[Description("/obj/item/clothing/neck/cloak")] brown_cloak__clothing_neck_cloak,

	[Description("/obj/item/clothing/neck/cloak/cap")] captains_cloak__neck_cloak_cap,

	[Description("/obj/item/clothing/neck/cloak/ce")] chief_engineers_cloak__neck_cloak_ce,

	[Description("/obj/item/clothing/neck/cloak/cmo")] chief_medical_officers_cloak__neck_cloak_cmo,

	[Description("/obj/item/clothing/neck/cloak/hop")] head_of_personnels_cloak__neck_cloak_hop,

	[Description("/obj/item/clothing/neck/cloak/hos")] head_of_securitys_cloak__neck_cloak_hos,

	[Description("/obj/item/clothing/neck/cloak/rd")] research_directors_cloak__neck_cloak_rd,

	[Description("/obj/item/clothing/neck/necklace/dope")] gold_necklace__neck_necklace_dope,

	[Description("/obj/item/clothing/neck/petcollar")] pet_collar__clothing_neck_petcollar,

	[Description("/obj/item/clothing/neck/scarf")] white_scarf__clothing_neck_scarf,

	[Description("/obj/item/clothing/neck/scarf/black")] black_scarf__neck_scarf_black,

	[Description("/obj/item/clothing/neck/scarf/christmas")] christmas_scarf__neck_scarf_christmas,

	[Description("/obj/item/clothing/neck/scarf/cyan")] cyan_scarf__neck_scarf_cyan,

	[Description("/obj/item/clothing/neck/scarf/darkblue")] dark_blue_scarf__neck_scarf_darkblue,

	[Description("/obj/item/clothing/neck/scarf/green")] green_scarf__neck_scarf_green,

	[Description("/obj/item/clothing/neck/scarf/orange")] orange_scarf__neck_scarf_orange,

	[Description("/obj/item/clothing/neck/scarf/purple")] purple_scarf__neck_scarf_purple,

	[Description("/obj/item/clothing/neck/scarf/red")] red_scarf__neck_scarf_red,

	[Description("/obj/item/clothing/neck/scarf/yellow")] yellow_scarf__neck_scarf_yellow,

	[Description("/obj/item/clothing/neck/scarf/zebra")] zebra_scarf__neck_scarf_zebra,

	[Description("/obj/item/clothing/neck/stethoscope")] stethoscope__clothing_neck_stethoscope,

	[Description("/obj/item/clothing/neck/stripedbluescarf")] striped_blue_scarf__clothing_neck_stripedbluescarf,

	[Description("/obj/item/clothing/neck/stripedgreenscarf")] striped_green_scarf__clothing_neck_stripedgreenscarf,

	[Description("/obj/item/clothing/neck/stripedredscarf")] striped_red_scarf__clothing_neck_stripedredscarf,

	[Description("/obj/item/clothing/neck/talisman")] bone_talisman__clothing_neck_talisman,

	[Description("/obj/item/clothing/neck/tie")] tie__clothing_neck_tie,

	[Description("/obj/item/clothing/neck/tie/black")] black_tie__neck_tie_black,

	[Description("/obj/item/clothing/neck/tie/blue")] blue_tie__neck_tie_blue,

	[Description("/obj/item/clothing/neck/tie/horrible")] horrible_tie__neck_tie_horrible,

	[Description("/obj/item/clothing/neck/tie/red")] red_tie__neck_tie_red,

	[Description("/obj/item/clothing/shoes/bhop")] jump_boots__clothing_shoes_bhop,

	[Description("/obj/item/clothing/shoes/chameleon")] black_shoes__clothing_shoes_chameleon,

	[Description("/obj/item/clothing/shoes/clockwork")] clockwork_treads__clothing_shoes_clockwork,

	[Description("/obj/item/clothing/shoes/clown_shoes")] clown_shoes__clothing_shoes_clown_shoes,

	[Description("/obj/item/clothing/shoes/clown_shoes/banana_shoes")] mk_honk_prototype_shoes__shoes_clown_shoes_banana_shoes,

	[Description("/obj/item/clothing/shoes/combat")] combat_boots__clothing_shoes_combat,

	[Description("/obj/item/clothing/shoes/cult")] nar_sian_invoker_boots__clothing_shoes_cult,

	[Description("/obj/item/clothing/shoes/cult/alt")] cultist_boots__shoes_cult_alt,

	[Description("/obj/item/clothing/shoes/cyborg")] cyborg_boots__clothing_shoes_cyborg,

	[Description("/obj/item/clothing/shoes/flightshoes")] flight_shoes__clothing_shoes_flightshoes,

	[Description("/obj/item/clothing/shoes/galoshes")] galoshes__clothing_shoes_galoshes,

	[Description("/obj/item/clothing/shoes/galoshes/dry")] absorbent_galoshes__shoes_galoshes_dry,

	[Description("/obj/item/clothing/shoes/gang")] Decorative_Brass_Knuckles__clothing_shoes_gang,

	[Description("/obj/item/clothing/shoes/golem")] golems_feet__clothing_shoes_golem,

	[Description("/obj/item/clothing/shoes/griffin")] griffon_boots__clothing_shoes_griffin,

	[Description("/obj/item/clothing/shoes/jackboots")] jackboots__clothing_shoes_jackboots,

	[Description("/obj/item/clothing/shoes/laceup")] laceup_shoes__clothing_shoes_laceup,

	[Description("/obj/item/clothing/shoes/magboots")] magboots__clothing_shoes_magboots,

	[Description("/obj/item/clothing/shoes/magboots/advance")] advanced_magboots__shoes_magboots_advance,

	[Description("/obj/item/clothing/shoes/magboots/syndie")] blood_red_magboots__shoes_magboots_syndie,

	[Description("/obj/item/clothing/shoes/plate")] Plate_Boots__clothing_shoes_plate,

	[Description("/obj/item/clothing/shoes/roman")] roman_sandals__clothing_shoes_roman,

	[Description("/obj/item/clothing/shoes/sandal")] sandals__clothing_shoes_sandal,

	[Description("/obj/item/clothing/shoes/sandal/marisa")] magic_shoes__shoes_sandal_marisa,

	[Description("/obj/item/clothing/shoes/singerb")] blue_performers_boots__clothing_shoes_singerb,

	[Description("/obj/item/clothing/shoes/singery")] yellow_performers_boots__clothing_shoes_singery,

	[Description("/obj/item/clothing/shoes/sneakers/black")] black_shoes__shoes_sneakers_black,

	[Description("/obj/item/clothing/shoes/sneakers/blue")] blue_shoes__shoes_sneakers_blue,

	[Description("/obj/item/clothing/shoes/sneakers/brown")] brown_shoes__shoes_sneakers_brown,

	[Description("/obj/item/clothing/shoes/sneakers/green")] green_shoes__shoes_sneakers_green,

	[Description("/obj/item/clothing/shoes/sneakers/mime")] mime_shoes__shoes_sneakers_mime,

	[Description("/obj/item/clothing/shoes/sneakers/orange")] orange_shoes__shoes_sneakers_orange,

	[Description("/obj/item/clothing/shoes/sneakers/purple")] purple_shoes__shoes_sneakers_purple,

	[Description("/obj/item/clothing/shoes/sneakers/rainbow")] rainbow_shoes__shoes_sneakers_rainbow,

	[Description("/obj/item/clothing/shoes/sneakers/red")] red_shoes__shoes_sneakers_red,

	[Description("/obj/item/clothing/shoes/sneakers/white")] white_shoes__shoes_sneakers_white,

	[Description("/obj/item/clothing/shoes/sneakers/yellow")] yellow_shoes__shoes_sneakers_yellow,

	[Description("/obj/item/clothing/shoes/space_ninja")] ninja_shoes__clothing_shoes_space_ninja,

	[Description("/obj/item/clothing/shoes/winterboots")] winter_boots__clothing_shoes_winterboots,

	[Description("/obj/item/clothing/shoes/workboots")] work_boots__clothing_shoes_workboots,

	[Description("/obj/item/clothing/shoes/workboots/mining")] mining_boots__shoes_workboots_mining,

	[Description("/obj/item/clothing/suit/apron")] apron__clothing_suit_apron,

	[Description("/obj/item/clothing/suit/apron/chef")] cooks_apron__suit_apron_chef,

	[Description("/obj/item/clothing/suit/apron/overalls")] coveralls__suit_apron_overalls,

	[Description("/obj/item/clothing/suit/apron/surgical")] surgical_apron__suit_apron_surgical,

	[Description("/obj/item/clothing/suit/armor/abductor/vest")] agent_vest__armor_abductor_vest,

	[Description("/obj/item/clothing/suit/armor/bone")] bone_armor__suit_armor_bone,

	[Description("/obj/item/clothing/suit/armor/bulletproof")] bulletproof_armor__suit_armor_bulletproof,

	[Description("/obj/item/clothing/suit/armor/centcom")] improper_Centcom_armor__suit_armor_centcom,

	[Description("/obj/item/clothing/suit/armor/changeling")] chitinous_mass__suit_armor_changeling,

	[Description("/obj/item/clothing/suit/armor/clockwork")] clockwork_cuirass__suit_armor_clockwork,

	[Description("/obj/item/clothing/suit/armor/heavy")] heavy_armor__suit_armor_heavy,

	[Description("/obj/item/clothing/suit/armor/hos")] armored_greatcoat__suit_armor_hos,

	[Description("/obj/item/clothing/suit/armor/hos/trenchcoat")] armored_trenchoat__armor_hos_trenchcoat,

	[Description("/obj/item/clothing/suit/armor/laserproof")] reflector_vest__suit_armor_laserproof,

	[Description("/obj/item/clothing/suit/armor/plate/crusader")] Crusaders_Armour__armor_plate_crusader,

	[Description("/obj/item/clothing/suit/armor/reactive")] reactive_armor__suit_armor_reactive,

	[Description("/obj/item/clothing/suit/armor/riot")] riot_suit__suit_armor_riot,

	[Description("/obj/item/clothing/suit/armor/riot/knight")] plate_armour__armor_riot_knight,

	[Description("/obj/item/clothing/suit/armor/riot/knight/templar")] crusader_armour__riot_knight_templar,

	[Description("/obj/item/clothing/suit/armor/tdome/green")] thunderdome_suit__armor_tdome_green,

	[Description("/obj/item/clothing/suit/armor/tdome/red")] thunderdome_suit__armor_tdome_red,

	[Description("/obj/item/clothing/suit/armor/vest")] armor_vest__suit_armor_vest,

	[Description("/obj/item/clothing/suit/armor/vest/capcarapace")] captains_carapace__armor_vest_capcarapace,

	[Description("/obj/item/clothing/suit/armor/vest/capcarapace/alt")] captains_parade_jacket__vest_capcarapace_alt,

	[Description("/obj/item/clothing/suit/armor/vest/capcarapace/syndicate")] syndicate_captains_vest__vest_capcarapace_syndicate,

	[Description("/obj/item/clothing/suit/armor/vest/det_suit")] detectives_armor_vest__armor_vest_det_suit,

	[Description("/obj/item/clothing/suit/armor/vest/leather")] security_overcoat__armor_vest_leather,

	[Description("/obj/item/clothing/suit/armor/vest/warden")] wardens_jacket__armor_vest_warden,

	[Description("/obj/item/clothing/suit/armor/vest/warden/alt")] wardens_armored_jacket__vest_warden_alt,

	[Description("/obj/item/clothing/suit/beekeeper_suit")] beekeeper_suit__clothing_suit_beekeeper_suit,

	[Description("/obj/item/clothing/suit/bio_suit")] bio_suit__clothing_suit_bio_suit,

	[Description("/obj/item/clothing/suit/bio_suit/plaguedoctorsuit")] plague_doctor_suit__suit_bio_suit_plaguedoctorsuit,

	[Description("/obj/item/clothing/suit/bluetag")] blue_laser_tag_armor__clothing_suit_bluetag,

	[Description("/obj/item/clothing/suit/bomb_suit")] bomb_suit__clothing_suit_bomb_suit,

	[Description("/obj/item/clothing/suit/bunnysuit")] Easter_Bunny_Suit__clothing_suit_bunnysuit,

	[Description("/obj/item/clothing/suit/captunic")] captains_parade_tunic__clothing_suit_captunic,

	[Description("/obj/item/clothing/suit/cardborg")] cardborg_suit__clothing_suit_cardborg,

	[Description("/obj/item/clothing/suit/chameleon")] armor__clothing_suit_chameleon,

	[Description("/obj/item/clothing/suit/chickensuit")] chicken_suit__clothing_suit_chickensuit,

	[Description("/obj/item/clothing/suit/cultrobes")] ancient_cultist_robes__clothing_suit_cultrobes,

	[Description("/obj/item/clothing/suit/cultrobes/alt")] cultist_robes__suit_cultrobes_alt,

	[Description("/obj/item/clothing/suit/curator")] treasure_hunters_coat__clothing_suit_curator,

	[Description("/obj/item/clothing/suit/cyborg_suit")] cyborg_suit__clothing_suit_cyborg_suit,

	[Description("/obj/item/clothing/suit/det_suit")] trenchcoat__clothing_suit_det_suit,

	[Description("/obj/item/clothing/suit/det_suit/grey")] noir_trenchcoat__suit_det_suit_grey,

	[Description("/obj/item/clothing/suit/fire")] emergency_firesuit__clothing_suit_fire,

	[Description("/obj/item/clothing/suit/fire/atmos")] firesuit__suit_fire_atmos,

	[Description("/obj/item/clothing/suit/fire/heavy")] heavy_firesuit__suit_fire_heavy,

	[Description("/obj/item/clothing/suit/golem")] adamantine_shell__clothing_suit_golem,

	[Description("/obj/item/clothing/suit/hastur")] improper_Hasturs_robe__clothing_suit_hastur,

	[Description("/obj/item/clothing/suit/hazardvest")] hazard_vest__clothing_suit_hazardvest,

	[Description("/obj/item/clothing/suit/holidaypriest")] holiday_priest__clothing_suit_holidaypriest,

	[Description("/obj/item/clothing/suit/hooded/bee_costume")] bee_costume__suit_hooded_bee_costume,

	[Description("/obj/item/clothing/suit/hooded/bloated_human")] bloated_human_suit__suit_hooded_bloated_human,

	[Description("/obj/item/clothing/suit/hooded/carp_costume")] carp_costume__suit_hooded_carp_costume,

	[Description("/obj/item/clothing/suit/hooded/chaplain_hoodie")] chaplain_hoodie__suit_hooded_chaplain_hoodie,

	[Description("/obj/item/clothing/suit/hooded/cloak/drake")] drake_armour__hooded_cloak_drake,

	[Description("/obj/item/clothing/suit/hooded/cloak/goliath")] goliath_cloak__hooded_cloak_goliath,

	[Description("/obj/item/clothing/suit/hooded/cultrobes/berserker")] flagellants_robes__hooded_cultrobes_berserker,

	[Description("/obj/item/clothing/suit/hooded/cultrobes/cult_shield")] empowered_cultist_armor__hooded_cultrobes_cult_shield,

	[Description("/obj/item/clothing/suit/hooded/explorer")] explorer_suit__suit_hooded_explorer,

	[Description("/obj/item/clothing/suit/hooded/ian_costume")] corgi_costume__suit_hooded_ian_costume,

	[Description("/obj/item/clothing/suit/hooded/wintercoat")] winter_coat__suit_hooded_wintercoat,

	[Description("/obj/item/clothing/suit/hooded/wintercoat/captain")] captains_winter_coat__hooded_wintercoat_captain,

	[Description("/obj/item/clothing/suit/hooded/wintercoat/cargo")] cargo_winter_coat__hooded_wintercoat_cargo,

	[Description("/obj/item/clothing/suit/hooded/wintercoat/engineering")] engineering_winter_coat__hooded_wintercoat_engineering,

	[Description("/obj/item/clothing/suit/hooded/wintercoat/engineering/atmos")] atmospherics_winter_coat__wintercoat_engineering_atmos,

	[Description("/obj/item/clothing/suit/hooded/wintercoat/hydro")] hydroponics_winter_coat__hooded_wintercoat_hydro,

	[Description("/obj/item/clothing/suit/hooded/wintercoat/medical")] medical_winter_coat__hooded_wintercoat_medical,

	[Description("/obj/item/clothing/suit/hooded/wintercoat/miner")] mining_winter_coat__hooded_wintercoat_miner,

	[Description("/obj/item/clothing/suit/hooded/wintercoat/science")] science_winter_coat__hooded_wintercoat_science,

	[Description("/obj/item/clothing/suit/hooded/wintercoat/security")] security_winter_coat__hooded_wintercoat_security,

	[Description("/obj/item/clothing/suit/ianshirt")] worn_shirt__clothing_suit_ianshirt,

	[Description("/obj/item/clothing/suit/imperium_monk")] improper_Imperium_monk_suit__clothing_suit_imperium_monk,

	[Description("/obj/item/clothing/suit/jacket")] bomber_jacket__clothing_suit_jacket,

	[Description("/obj/item/clothing/suit/jacket/leather")] leather_jacket__suit_jacket_leather,

	[Description("/obj/item/clothing/suit/jacket/leather/overcoat")] leather_overcoat__jacket_leather_overcoat,

	[Description("/obj/item/clothing/suit/jacket/letterman")] letterman_jacket__suit_jacket_letterman,

	[Description("/obj/item/clothing/suit/jacket/letterman_nanotrasen")] blue_letterman_jacket__suit_jacket_letterman_nanotrasen,

	[Description("/obj/item/clothing/suit/jacket/letterman_red")] red_letterman_jacket__suit_jacket_letterman_red,

	[Description("/obj/item/clothing/suit/jacket/letterman_syndie")] blood_red_letterman_jacket__suit_jacket_letterman_syndie,

	[Description("/obj/item/clothing/suit/jacket/miljacket")] military_jacket__suit_jacket_miljacket,

	[Description("/obj/item/clothing/suit/jacket/puffer")] puffer_jacket__suit_jacket_puffer,

	[Description("/obj/item/clothing/suit/jacket/puffer/vest")] puffer_vest__jacket_puffer_vest,

	[Description("/obj/item/clothing/suit/judgerobe")] judges_robe__clothing_suit_judgerobe,

	[Description("/obj/item/clothing/suit/justice")] justice_suit__clothing_suit_justice,

	[Description("/obj/item/clothing/suit/magusred")] magus_robes__clothing_suit_magusred,

	[Description("/obj/item/clothing/suit/monkeysuit")] monkey_suit__clothing_suit_monkeysuit,

	[Description("/obj/item/clothing/suit/nerdshirt")] gamer_shirt__clothing_suit_nerdshirt,

	[Description("/obj/item/clothing/suit/nun")] nun_robe__clothing_suit_nun,

	[Description("/obj/item/clothing/suit/pirate")] pirate_coat__clothing_suit_pirate,

	[Description("/obj/item/clothing/suit/pirate/captain")] pirate_captain_coat__suit_pirate_captain,

	[Description("/obj/item/clothing/suit/poncho")] poncho__clothing_suit_poncho,

	[Description("/obj/item/clothing/suit/poncho/green")] green_poncho__suit_poncho_green,

	[Description("/obj/item/clothing/suit/poncho/ponchoshame")] poncho_of_shame__suit_poncho_ponchoshame,

	[Description("/obj/item/clothing/suit/poncho/red")] red_poncho__suit_poncho_red,

	[Description("/obj/item/clothing/suit/radiation")] radiation_suit__clothing_suit_radiation,

	[Description("/obj/item/clothing/suit/redtag")] red_laser_tag_armor__clothing_suit_redtag,

	[Description("/obj/item/clothing/suit/security/hos")] head_of_securitys_jacket__suit_security_hos,

	[Description("/obj/item/clothing/suit/security/officer")] security_officers_jacket__suit_security_officer,

	[Description("/obj/item/clothing/suit/security/officer/russian")] russian_officers_jacket__security_officer_russian,

	[Description("/obj/item/clothing/suit/security/warden")] wardens_jacket__suit_security_warden,

	[Description("/obj/item/clothing/suit/snowman")] snowman_outfit__clothing_suit_snowman,

	[Description("/obj/item/clothing/suit/space")] space_suit__clothing_suit_space,

	[Description("/obj/item/clothing/suit/space/changeling")] flesh_mass__suit_space_changeling,

	[Description("/obj/item/clothing/suit/space/chronos")] Chronosuit__suit_space_chronos,

	[Description("/obj/item/clothing/suit/space/eva")] EVA_suit__suit_space_eva,

	[Description("/obj/item/clothing/suit/space/eva/plasmaman")] EVA_plasma_envirosuit__space_eva_plasmaman,

	[Description("/obj/item/clothing/suit/space/fragile")] emergency_space_suit__suit_space_fragile,

	[Description("/obj/item/clothing/suit/space/freedom")] eagle_suit__suit_space_freedom,

	[Description("/obj/item/clothing/suit/space/hardsuit")] hardsuit__suit_space_hardsuit,

	[Description("/obj/item/clothing/suit/space/hardsuit/captain")] captains_SWAT_suit__space_hardsuit_captain,

	[Description("/obj/item/clothing/suit/space/hardsuit/carp")] carp_space_suit__space_hardsuit_carp,

	[Description("/obj/item/clothing/suit/space/hardsuit/clown")] cosmohonk_hardsuit__space_hardsuit_clown,

	[Description("/obj/item/clothing/suit/space/hardsuit/cult")] nar_sien_hardened_armor__space_hardsuit_cult,

	[Description("/obj/item/clothing/suit/space/hardsuit/deathsquad")] MKIII_SWAT_Suit__space_hardsuit_deathsquad,

	[Description("/obj/item/clothing/suit/space/hardsuit/engine")] engineering_hardsuit__space_hardsuit_engine,

	[Description("/obj/item/clothing/suit/space/hardsuit/engine/atmos")] atmospherics_hardsuit__hardsuit_engine_atmos,

	[Description("/obj/item/clothing/suit/space/hardsuit/engine/elite")] advanced_hardsuit__hardsuit_engine_elite,

	[Description("/obj/item/clothing/suit/space/hardsuit/ert")] emergency_response_team_suit__space_hardsuit_ert,

	[Description("/obj/item/clothing/suit/space/hardsuit/ert/paranormal")] paranormal_response_team_suit__hardsuit_ert_paranormal,

	[Description("/obj/item/clothing/suit/space/hardsuit/ert/paranormal/beserker")] champions_hardsuit__ert_paranormal_beserker,

	[Description("/obj/item/clothing/suit/space/hardsuit/ert/paranormal/inquisitor")] inquisitors_hardsuit__ert_paranormal_inquisitor,

	[Description("/obj/item/clothing/suit/space/hardsuit/flightsuit")] flight_suit__space_hardsuit_flightsuit,

	[Description("/obj/item/clothing/suit/space/hardsuit/medical")] medical_hardsuit__space_hardsuit_medical,

	[Description("/obj/item/clothing/suit/space/hardsuit/mining")] mining_hardsuit__space_hardsuit_mining,

	[Description("/obj/item/clothing/suit/space/hardsuit/rd")] prototype_hardsuit__space_hardsuit_rd,

	[Description("/obj/item/clothing/suit/space/hardsuit/security")] security_hardsuit__space_hardsuit_security,

	[Description("/obj/item/clothing/suit/space/hardsuit/security/hos")] head_of_securitys_hardsuit__hardsuit_security_hos,

	[Description("/obj/item/clothing/suit/space/hardsuit/shielded")] shielded_hardsuit__space_hardsuit_shielded,

	[Description("/obj/item/clothing/suit/space/hardsuit/shielded/ctf")] white_shielded_hardsuit__hardsuit_shielded_ctf,

	[Description("/obj/item/clothing/suit/space/hardsuit/shielded/ctf/blue")] blue_shielded_hardsuit__shielded_ctf_blue,

	[Description("/obj/item/clothing/suit/space/hardsuit/shielded/ctf/red")] red_shielded_hardsuit__shielded_ctf_red,

	[Description("/obj/item/clothing/suit/space/hardsuit/shielded/swat")] death_commando_spacesuit__hardsuit_shielded_swat,

	[Description("/obj/item/clothing/suit/space/hardsuit/shielded/syndi")] blood_red_hardsuit__hardsuit_shielded_syndi,

	[Description("/obj/item/clothing/suit/space/hardsuit/shielded/wizard")] battlemage_armour__hardsuit_shielded_wizard,

	[Description("/obj/item/clothing/suit/space/hardsuit/syndi")] blood_red_hardsuit__space_hardsuit_syndi,

	[Description("/obj/item/clothing/suit/space/hardsuit/syndi/elite")] elite_syndicate_hardsuit__hardsuit_syndi_elite,

	[Description("/obj/item/clothing/suit/space/hardsuit/syndi/owl")] owl_hardsuit__hardsuit_syndi_owl,

	[Description("/obj/item/clothing/suit/space/hardsuit/ueg")] Iron_Hawk_Marine_Armour__space_hardsuit_ueg,

	[Description("/obj/item/clothing/suit/space/hardsuit/wizard")] gem_encrusted_hardsuit__space_hardsuit_wizard,

	[Description("/obj/item/clothing/suit/space/nasavoid")] NASA_Voidsuit__suit_space_nasavoid,

	[Description("/obj/item/clothing/suit/space/officer")] officers_jacket__suit_space_officer,

	[Description("/obj/item/clothing/suit/space/orange")] emergency_space_suit__suit_space_orange,

	[Description("/obj/item/clothing/suit/space/pirate")] pirate_coat__suit_space_pirate,

	[Description("/obj/item/clothing/suit/space/santa")] Santas_suit__suit_space_santa,

	[Description("/obj/item/clothing/suit/space/space_ninja")] ninja_suit__suit_space_space_ninja,

	[Description("/obj/item/clothing/suit/space/swat")] MKI_SWAT_Suit__suit_space_swat,

	[Description("/obj/item/clothing/suit/space/syndicate")] red_space_suit__suit_space_syndicate,

	[Description("/obj/item/clothing/suit/space/syndicate/black")] black_space_suit__space_syndicate_black,

	[Description("/obj/item/clothing/suit/space/syndicate/black/blue")] black_and_blue_space_suit__syndicate_black_blue,

	[Description("/obj/item/clothing/suit/space/syndicate/black/engie")] black_engineering_space_suit__syndicate_black_engie,

	[Description("/obj/item/clothing/suit/space/syndicate/black/green")] black_and_green_space_suit__syndicate_black_green,

	[Description("/obj/item/clothing/suit/space/syndicate/black/med")] green_space_suit__syndicate_black_med,

	[Description("/obj/item/clothing/suit/space/syndicate/black/orange")] black_and_orange_space_suit__syndicate_black_orange,

	[Description("/obj/item/clothing/suit/space/syndicate/black/red")] black_and_red_space_suit__syndicate_black_red,

	[Description("/obj/item/clothing/suit/space/syndicate/blue")] blue_space_suit__space_syndicate_blue,

	[Description("/obj/item/clothing/suit/space/syndicate/green")] green_space_suit__space_syndicate_green,

	[Description("/obj/item/clothing/suit/space/syndicate/green/dark")] dark_green_space_suit__syndicate_green_dark,

	[Description("/obj/item/clothing/suit/space/syndicate/orange")] orange_space_suit__space_syndicate_orange,

	[Description("/obj/item/clothing/suit/spookyghost")] spooky_ghost__clothing_suit_spookyghost,

	[Description("/obj/item/clothing/suit/straight_jacket")] straight_jacket__clothing_suit_straight_jacket,

	[Description("/obj/item/clothing/suit/studentuni")] student_robe__clothing_suit_studentuni,

	[Description("/obj/item/clothing/suit/suspenders")] suspenders__clothing_suit_suspenders,

	[Description("/obj/item/clothing/suit/syndicatefake")] black_and_red_space_suit_replica__clothing_suit_syndicatefake,

	[Description("/obj/item/clothing/suit/toggle/chef")] chefs_apron__suit_toggle_chef,

	[Description("/obj/item/clothing/suit/toggle/labcoat")] labcoat__suit_toggle_labcoat,

	[Description("/obj/item/clothing/suit/toggle/labcoat/chemist")] chemist_labcoat__toggle_labcoat_chemist,

	[Description("/obj/item/clothing/suit/toggle/labcoat/cmo")] chief_medical_officers_labcoat__toggle_labcoat_cmo,

	[Description("/obj/item/clothing/suit/toggle/labcoat/emt")] EMTs_jacket__toggle_labcoat_emt,

	[Description("/obj/item/clothing/suit/toggle/labcoat/genetics")] geneticist_labcoat__toggle_labcoat_genetics,

	[Description("/obj/item/clothing/suit/toggle/labcoat/mad")] improper_The_Mads_labcoat__toggle_labcoat_mad,

	[Description("/obj/item/clothing/suit/toggle/labcoat/science")] scientist_labcoat__toggle_labcoat_science,

	[Description("/obj/item/clothing/suit/toggle/labcoat/virologist")] virologist_labcoat__toggle_labcoat_virologist,

	[Description("/obj/item/clothing/suit/toggle/lawyer")] blue_suit_jacket__suit_toggle_lawyer,

	[Description("/obj/item/clothing/suit/toggle/lawyer/black")] black_suit_jacket__toggle_lawyer_black,

	[Description("/obj/item/clothing/suit/toggle/lawyer/purple")] purple_suit_jacket__toggle_lawyer_purple,

	[Description("/obj/item/clothing/suit/toggle/owlwings")] owl_cloak__suit_toggle_owlwings,

	[Description("/obj/item/clothing/suit/toggle/owlwings/griffinwings")] griffon_cloak__toggle_owlwings_griffinwings,

	[Description("/obj/item/clothing/suit/vapeshirt")] Vape_Naysh_shirt__clothing_suit_vapeshirt,

	[Description("/obj/item/clothing/suit/whitedress")] white_dress__clothing_suit_whitedress,

	[Description("/obj/item/clothing/suit/witchhunter")] witchunter_garb__clothing_suit_witchhunter,

	[Description("/obj/item/clothing/suit/wizrobe")] wizard_robe__clothing_suit_wizrobe,

	[Description("/obj/item/clothing/suit/wizrobe/black")] black_wizard_robe__suit_wizrobe_black,

	[Description("/obj/item/clothing/suit/wizrobe/fake")] wizard_robe__suit_wizrobe_fake,

	[Description("/obj/item/clothing/suit/wizrobe/magusblue")] improper_Magus_robe__suit_wizrobe_magusblue,

	[Description("/obj/item/clothing/suit/wizrobe/magusred")] improper_Magus_robe__suit_wizrobe_magusred,

	[Description("/obj/item/clothing/suit/wizrobe/marisa")] witch_robe__suit_wizrobe_marisa,

	[Description("/obj/item/clothing/suit/wizrobe/marisa/fake")] witch_robe__wizrobe_marisa_fake,

	[Description("/obj/item/clothing/suit/wizrobe/paper")] papier_mache_robe__suit_wizrobe_paper,

	[Description("/obj/item/clothing/suit/wizrobe/red")] red_wizard_robe__suit_wizrobe_red,

	[Description("/obj/item/clothing/suit/wizrobe/santa")] Santas_suit__suit_wizrobe_santa,

	[Description("/obj/item/clothing/suit/wizrobe/yellow")] yellow_wizard_robe__suit_wizrobe_yellow,

	[Description("/obj/item/clothing/suit/xenos")] xenos_suit__clothing_suit_xenos,

	[Description("/obj/item/clothing/tie")] tie__item_clothing_tie,

	[Description("/obj/item/clothing/tie/armband")] red_armband__clothing_tie_armband,

	[Description("/obj/item/clothing/tie/armband/cargo")] cargo_bay_guard_armband__tie_armband_cargo,

	[Description("/obj/item/clothing/tie/armband/engine")] engineering_guard_armband__tie_armband_engine,

	[Description("/obj/item/clothing/tie/armband/hydro")] hydroponics_guard_armband__tie_armband_hydro,

	[Description("/obj/item/clothing/tie/armband/med")] medical_guard_armband__tie_armband_med,

	[Description("/obj/item/clothing/tie/armband/medblue")] medical_guard_armband__tie_armband_medblue,

	[Description("/obj/item/clothing/tie/armband/science")] science_guard_armband__tie_armband_science,

	[Description("/obj/item/clothing/tie/lawyers_badge")] attorneys_badge__clothing_tie_lawyers_badge,

	[Description("/obj/item/clothing/tie/medal")] bronze_medal__clothing_tie_medal,

	[Description("/obj/item/clothing/tie/medal/bronze_heart")] bronze_heart_medal__tie_medal_bronze_heart,

	[Description("/obj/item/clothing/tie/medal/gold")] gold_medal__tie_medal_gold,

	[Description("/obj/item/clothing/tie/medal/silver")] silver_medal__tie_medal_silver,

	[Description("/obj/item/clothing/tie/waistcoat")] waistcoat__clothing_tie_waistcoat,

	[Description("/obj/item/clothing/under/acj")] administrative_cybernetic_jumpsuit__clothing_under_acj,

	[Description("/obj/item/clothing/under/assistantformal")] assistants_formal_uniform__clothing_under_assistantformal,

	[Description("/obj/item/clothing/under/blacktango")] black_tango_dress__clothing_under_blacktango,

	[Description("/obj/item/clothing/under/burial")] burial_garments__clothing_under_burial,

	[Description("/obj/item/clothing/under/captainparade")] captains_parade_uniform__clothing_under_captainparade,

	[Description("/obj/item/clothing/under/chameleon")] black_jumpsuit__clothing_under_chameleon,

	[Description("/obj/item/clothing/under/cloud")] cloud__clothing_under_cloud,

	[Description("/obj/item/clothing/under/color/black")] black_jumpsuit__under_color_black,

	[Description("/obj/item/clothing/under/color/blue")] blue_jumpsuit__under_color_blue,

	[Description("/obj/item/clothing/under/color/brown")] brown_jumpsuit__under_color_brown,

	[Description("/obj/item/clothing/under/color/darkblue")] darkblue_jumpsuit__under_color_darkblue,

	[Description("/obj/item/clothing/under/color/darkgreen")] darkgreen_jumpsuit__under_color_darkgreen,

	[Description("/obj/item/clothing/under/color/green")] green_jumpsuit__under_color_green,

	[Description("/obj/item/clothing/under/color/grey")] grey_jumpsuit__under_color_grey,

	[Description("/obj/item/clothing/under/color/lightbrown")] lightbrown_jumpsuit__under_color_lightbrown,

	[Description("/obj/item/clothing/under/color/lightpurple")] purple_jumpsuit__under_color_lightpurple,

	[Description("/obj/item/clothing/under/color/maroon")] maroon_jumpsuit__under_color_maroon,

	[Description("/obj/item/clothing/under/color/orange")] orange_jumpsuit__under_color_orange,

	[Description("/obj/item/clothing/under/color/pink")] pink_jumpsuit__under_color_pink,

	[Description("/obj/item/clothing/under/color/rainbow")] rainbow_jumpsuit__under_color_rainbow,

	[Description("/obj/item/clothing/under/color/red")] red_jumpsuit__under_color_red,

	[Description("/obj/item/clothing/under/color/teal")] teal_jumpsuit__under_color_teal,

	[Description("/obj/item/clothing/under/color/white")] white_jumpsuit__under_color_white,

	[Description("/obj/item/clothing/under/color/yellow")] yellow_jumpsuit__under_color_yellow,

	[Description("/obj/item/clothing/under/geisha")] geisha_suit__clothing_under_geisha,

	[Description("/obj/item/clothing/under/gimmick/rank/captain/suit")] captains_suit__rank_captain_suit,

	[Description("/obj/item/clothing/under/gimmick/rank/head_of_personnel/suit")] head_of_personnels_suit__rank_head_of_personnel_suit,

	[Description("/obj/item/clothing/under/gladiator")] gladiator_uniform__clothing_under_gladiator,

	[Description("/obj/item/clothing/under/golem")] adamantine_skin__clothing_under_golem,

	[Description("/obj/item/clothing/under/griffin")] griffon_uniform__clothing_under_griffin,

	[Description("/obj/item/clothing/under/hosparadefem")] head_of_securitys_parade_uniform__clothing_under_hosparadefem,

	[Description("/obj/item/clothing/under/hosparademale")] head_of_securitys_parade_uniform__clothing_under_hosparademale,

	[Description("/obj/item/clothing/under/jabroni")] Jabroni_Outfit__clothing_under_jabroni,

	[Description("/obj/item/clothing/under/janimaid")] maid_uniform__clothing_under_janimaid,

	[Description("/obj/item/clothing/under/jester")] jester_suit__clothing_under_jester,

	[Description("/obj/item/clothing/under/kilt")] kilt__clothing_under_kilt,

	[Description("/obj/item/clothing/under/lawyer/blacksuit")] black_suit__under_lawyer_blacksuit,

	[Description("/obj/item/clothing/under/lawyer/bluesuit")] blue_suit__under_lawyer_bluesuit,

	[Description("/obj/item/clothing/under/lawyer/purpsuit")] purple_suit__under_lawyer_purpsuit,

	[Description("/obj/item/clothing/under/maid")] maid_costume__clothing_under_maid,

	[Description("/obj/item/clothing/under/overalls")] laborers_overalls__clothing_under_overalls,

	[Description("/obj/item/clothing/under/owl")] owl_uniform__clothing_under_owl,

	[Description("/obj/item/clothing/under/pants/black")] black_pants__under_pants_black,

	[Description("/obj/item/clothing/under/pants/blackjeans")] black_jeans__under_pants_blackjeans,

	[Description("/obj/item/clothing/under/pants/camo")] camo_pants__under_pants_camo,

	[Description("/obj/item/clothing/under/pants/classicjeans")] classic_jeans__under_pants_classicjeans,

	[Description("/obj/item/clothing/under/pants/jeans")] jeans__under_pants_jeans,

	[Description("/obj/item/clothing/under/pants/khaki")] khaki_pants__under_pants_khaki,

	[Description("/obj/item/clothing/under/pants/mustangjeans")] Must_Hang_jeans__under_pants_mustangjeans,

	[Description("/obj/item/clothing/under/pants/red")] red_pants__under_pants_red,

	[Description("/obj/item/clothing/under/pants/tan")] tan_pants__under_pants_tan,

	[Description("/obj/item/clothing/under/pants/track")] track_pants__under_pants_track,

	[Description("/obj/item/clothing/under/pants/white")] white_pants__under_pants_white,

	[Description("/obj/item/clothing/under/pants/youngfolksjeans")] Young_Folks_jeans__under_pants_youngfolksjeans,

	[Description("/obj/item/clothing/under/patriotsuit")] Patriotic_Suit__clothing_under_patriotsuit,

	[Description("/obj/item/clothing/under/pirate")] pirate_outfit__clothing_under_pirate,

	[Description("/obj/item/clothing/under/pj/blue")] blue_pjs__under_pj_blue,

	[Description("/obj/item/clothing/under/pj/red")] red_pjs__under_pj_red,

	[Description("/obj/item/clothing/under/plaid_skirt")] red_plaid_skirt__clothing_under_plaid_skirt,

	[Description("/obj/item/clothing/under/plaid_skirt/blue")] blue_plaid_skirt__under_plaid_skirt_blue,

	[Description("/obj/item/clothing/under/plaid_skirt/green")] green_plaid_skirt__under_plaid_skirt_green,

	[Description("/obj/item/clothing/under/plaid_skirt/purple")] purple_plaid_skirt__under_plaid_skirt_purple,

	[Description("/obj/item/clothing/under/plasmaman")] plasma_envirosuit__clothing_under_plasmaman,

	[Description("/obj/item/clothing/under/rank/atmospheric_technician")] atmospheric_technicians_jumpsuit__under_rank_atmospheric_technician,

	[Description("/obj/item/clothing/under/rank/bartender")] bartenders_uniform__under_rank_bartender,

	[Description("/obj/item/clothing/under/rank/captain")] captains_jumpsuit__under_rank_captain,

	[Description("/obj/item/clothing/under/rank/cargo")] quartermasters_jumpsuit__under_rank_cargo,

	[Description("/obj/item/clothing/under/rank/cargotech")] cargo_technicians_jumpsuit__under_rank_cargotech,

	[Description("/obj/item/clothing/under/rank/centcom_commander")] improper_Centcom_officers_jumpsuit__under_rank_centcom_commander,

	[Description("/obj/item/clothing/under/rank/centcom_officer")] improper_Centcom_officers_jumpsuit__under_rank_centcom_officer,

	[Description("/obj/item/clothing/under/rank/chaplain")] chaplains_jumpsuit__under_rank_chaplain,

	[Description("/obj/item/clothing/under/rank/chef")] cooks_suit__under_rank_chef,

	[Description("/obj/item/clothing/under/rank/chemist")] chemists_jumpsuit__under_rank_chemist,

	[Description("/obj/item/clothing/under/rank/chief_engineer")] chief_engineers_jumpsuit__under_rank_chief_engineer,

	[Description("/obj/item/clothing/under/rank/chief_medical_officer")] chief_medical_officers_jumpsuit__under_rank_chief_medical_officer,

	[Description("/obj/item/clothing/under/rank/clown")] clown_suit__under_rank_clown,

	[Description("/obj/item/clothing/under/rank/clown/sexy")] sexy_clown_suit__rank_clown_sexy,

	[Description("/obj/item/clothing/under/rank/curator")] sensible_suit__under_rank_curator,

	[Description("/obj/item/clothing/under/rank/curator/treasure_hunter")] treasure_hunter_uniform__rank_curator_treasure_hunter,

	[Description("/obj/item/clothing/under/rank/det")] hard_worn_suit__under_rank_det,

	[Description("/obj/item/clothing/under/rank/det/grey")] noir_suit__rank_det_grey,

	[Description("/obj/item/clothing/under/rank/engineer")] engineers_jumpsuit__under_rank_engineer,

	[Description("/obj/item/clothing/under/rank/geneticist")] geneticists_jumpsuit__under_rank_geneticist,

	[Description("/obj/item/clothing/under/rank/head_of_personnel")] head_of_personnels_jumpsuit__under_rank_head_of_personnel,

	[Description("/obj/item/clothing/under/rank/head_of_security")] head_of_securitys_jumpsuit__under_rank_head_of_security,

	[Description("/obj/item/clothing/under/rank/head_of_security/alt")] head_of_securitys_turtleneck__rank_head_of_security_alt,

	[Description("/obj/item/clothing/under/rank/head_of_security/grey")] head_of_securitys_grey_jumpsuit__rank_head_of_security_grey,

	[Description("/obj/item/clothing/under/rank/head_of_security/navyblue")] head_of_securitys_formal_uniform__rank_head_of_security_navyblue,

	[Description("/obj/item/clothing/under/rank/hydroponics")] botanists_jumpsuit__under_rank_hydroponics,

	[Description("/obj/item/clothing/under/rank/janitor")] janitors_jumpsuit__under_rank_janitor,

	[Description("/obj/item/clothing/under/rank/mailman")] mailmans_jumpsuit__under_rank_mailman,

	[Description("/obj/item/clothing/under/rank/medical")] medical_doctors_jumpsuit__under_rank_medical,

	[Description("/obj/item/clothing/under/rank/medical/blue")] medical_scrubs__rank_medical_blue,

	[Description("/obj/item/clothing/under/rank/medical/green")] medical_scrubs__rank_medical_green,

	[Description("/obj/item/clothing/under/rank/medical/purple")] medical_scrubs__rank_medical_purple,

	[Description("/obj/item/clothing/under/rank/mime")] mimes_outfit__under_rank_mime,

	[Description("/obj/item/clothing/under/rank/miner")] shaft_miners_jumpsuit__under_rank_miner,

	[Description("/obj/item/clothing/under/rank/miner/lavaland")] shaft_miners_jumpsuit__rank_miner_lavaland,

	[Description("/obj/item/clothing/under/rank/nursesuit")] nurses_suit__under_rank_nursesuit,

	[Description("/obj/item/clothing/under/rank/prisoner")] prison_jumpsuit__under_rank_prisoner,

	[Description("/obj/item/clothing/under/rank/psyche")] psychedelic_jumpsuit__under_rank_psyche,

	[Description("/obj/item/clothing/under/rank/research_director")] research_directors_vest_suit__under_rank_research_director,

	[Description("/obj/item/clothing/under/rank/research_director/alt")] research_directors_tan_suit__rank_research_director_alt,

	[Description("/obj/item/clothing/under/rank/research_director/turtleneck")] research_directors_turtleneck__rank_research_director_turtleneck,

	[Description("/obj/item/clothing/under/rank/roboticist")] roboticists_jumpsuit__under_rank_roboticist,

	[Description("/obj/item/clothing/under/rank/scientist")] scientists_jumpsuit__under_rank_scientist,

	[Description("/obj/item/clothing/under/rank/security")] security_jumpsuit__under_rank_security,

	[Description("/obj/item/clothing/under/rank/security/grey")] grey_security_jumpsuit__rank_security_grey,

	[Description("/obj/item/clothing/under/rank/security/navyblue")] security_officers_formal_uniform__rank_security_navyblue,

	[Description("/obj/item/clothing/under/rank/security/navyblue/russian")] russian_officers_uniform__security_navyblue_russian,

	[Description("/obj/item/clothing/under/rank/vice")] vice_officers_jumpsuit__under_rank_vice,

	[Description("/obj/item/clothing/under/rank/virologist")] virologists_jumpsuit__under_rank_virologist,

	[Description("/obj/item/clothing/under/rank/warden")] security_suit__under_rank_warden,

	[Description("/obj/item/clothing/under/rank/warden/grey")] grey_security_suit__rank_warden_grey,

	[Description("/obj/item/clothing/under/rank/warden/navyblue")] wardens_formal_uniform__rank_warden_navyblue,

	[Description("/obj/item/clothing/under/redcoat")] redcoat_uniform__clothing_under_redcoat,

	[Description("/obj/item/clothing/under/redeveninggown")] red_evening_gown__clothing_under_redeveninggown,

	[Description("/obj/item/clothing/under/roman")] roman_armor__clothing_under_roman,

	[Description("/obj/item/clothing/under/sailor")] sailor_suit__clothing_under_sailor,

	[Description("/obj/item/clothing/under/sailordress")] sailor_dress__clothing_under_sailordress,

	[Description("/obj/item/clothing/under/schoolgirl")] blue_schoolgirl_uniform__clothing_under_schoolgirl,

	[Description("/obj/item/clothing/under/schoolgirl/green")] green_schoolgirl_uniform__under_schoolgirl_green,

	[Description("/obj/item/clothing/under/schoolgirl/orange")] orange_schoolgirl_uniform__under_schoolgirl_orange,

	[Description("/obj/item/clothing/under/schoolgirl/red")] red_schoolgirl_uniform__under_schoolgirl_red,

	[Description("/obj/item/clothing/under/scratch")] white_suit__clothing_under_scratch,

	[Description("/obj/item/clothing/under/sexymime")] sexy_mime_outfit__clothing_under_sexymime,

	[Description("/obj/item/clothing/under/shorts/black")] black_athletic_shorts__under_shorts_black,

	[Description("/obj/item/clothing/under/shorts/blue")] blue_athletic_shorts__under_shorts_blue,

	[Description("/obj/item/clothing/under/shorts/green")] green_athletic_shorts__under_shorts_green,

	[Description("/obj/item/clothing/under/shorts/grey")] grey_athletic_shorts__under_shorts_grey,

	[Description("/obj/item/clothing/under/shorts/purple")] purple_athletic_shorts__under_shorts_purple,

	[Description("/obj/item/clothing/under/shorts/red")] red_athletic_shorts__under_shorts_red,

	[Description("/obj/item/clothing/under/singerb")] blue_performers_outfit__clothing_under_singerb,

	[Description("/obj/item/clothing/under/singery")] yellow_performers_outfit__clothing_under_singery,

	[Description("/obj/item/clothing/under/skirt/black")] black_skirt__under_skirt_black,

	[Description("/obj/item/clothing/under/skirt/blue")] blue_skirt__under_skirt_blue,

	[Description("/obj/item/clothing/under/skirt/purple")] purple_skirt__under_skirt_purple,

	[Description("/obj/item/clothing/under/skirt/red")] red_skirt__under_skirt_red,

	[Description("/obj/item/clothing/under/sl_suit")] amish_suit__clothing_under_sl_suit,

	[Description("/obj/item/clothing/under/soviet")] soviet_uniform__clothing_under_soviet,

	[Description("/obj/item/clothing/under/space")] improper_NASA_jumpsuit__clothing_under_space,

	[Description("/obj/item/clothing/under/stripeddress")] striped_dress__clothing_under_stripeddress,

	[Description("/obj/item/clothing/under/suit_jacket")] black_suit__clothing_under_suit_jacket,

	[Description("/obj/item/clothing/under/suit_jacket/burgundy")] burgundy_suit__under_suit_jacket_burgundy,

	[Description("/obj/item/clothing/under/suit_jacket/charcoal")] charcoal_suit__under_suit_jacket_charcoal,

	[Description("/obj/item/clothing/under/suit_jacket/checkered")] checkered_suit__under_suit_jacket_checkered,

	[Description("/obj/item/clothing/under/suit_jacket/female")] executive_suit__under_suit_jacket_female,

	[Description("/obj/item/clothing/under/suit_jacket/green")] green_suit__under_suit_jacket_green,

	[Description("/obj/item/clothing/under/suit_jacket/navy")] navy_suit__under_suit_jacket_navy,

	[Description("/obj/item/clothing/under/suit_jacket/really_black")] executive_suit__under_suit_jacket_really_black,

	[Description("/obj/item/clothing/under/suit_jacket/red")] red_suit__under_suit_jacket_red,

	[Description("/obj/item/clothing/under/suit_jacket/tan")] tan_suit__under_suit_jacket_tan,

	[Description("/obj/item/clothing/under/suit_jacket/white")] white_suit__under_suit_jacket_white,

	[Description("/obj/item/clothing/under/sundress")] sundress__clothing_under_sundress,

	[Description("/obj/item/clothing/under/syndicate")] tactical_turtleneck__clothing_under_syndicate,

	[Description("/obj/item/clothing/under/syndicate/camo")] camouflage_fatigues__under_syndicate_camo,

	[Description("/obj/item/clothing/under/syndicate/sniper")] Tactical_turtleneck_suit__under_syndicate_sniper,

	[Description("/obj/item/clothing/under/syndicate/soviet")] Ratnik_5_tracksuit__under_syndicate_soviet,

	[Description("/obj/item/clothing/under/syndicate/tacticool")] tacticool_turtleneck__under_syndicate_tacticool,

	[Description("/obj/item/clothing/under/trek/Q")] french_marshalls_uniform__under_trek_Q,

	[Description("/obj/item/clothing/under/trek/command")] command_uniform__under_trek_command,

	[Description("/obj/item/clothing/under/trek/engsec")] engsec_uniform__under_trek_engsec,

	[Description("/obj/item/clothing/under/trek/medsci")] medsci_uniform__under_trek_medsci,

	[Description("/obj/item/clothing/under/villain")] villain_suit__clothing_under_villain,

	[Description("/obj/item/clothing/under/waiter")] waiters_outfit__clothing_under_waiter
}