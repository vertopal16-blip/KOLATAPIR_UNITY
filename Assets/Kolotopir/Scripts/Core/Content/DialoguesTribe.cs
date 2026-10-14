using System;

namespace Kolotopir.Core
{
    public static partial class GameDb
    {
        private const string Gina = "Abuela Gina";
        private const string Oren = "Anciano Oren";
        private const string Haro = "Haro";
        private const string Lia = "Lía";
        private const string Tome = "Tomé";
        private const string Suri = "Suri";
        private const string Kiko = "Kiko";

        private static bool Daily(GameState s, string key) => s.Flags.Get(key) != s.Clock.Day;
        private static void MarkDaily(IGameContext ctx, string key) => ctx.State.Flags.SetVar(key, ctx.State.Clock.Day);

        private static void AddTribeDialogues()
        {
            // ================================================= GINA
            var b = new DialogueBuilder("gina_intro");
            b.Say("start", Gina, "¡Por fin despiertas, dormilón! El sol ya ha llegado hasta la olla y tú todavía soñando.", Emotion.Alegre)
             .Enter(ctx => ctx.Command("encuadre:gina"));
            b.Say("n2", Gina, "Anoche la Cápsula de la hondonada zumbó otra vez. Como la noche en que tus padres se marcharon. Todo el mundo lo oyó, aunque nadie lo diga en voz alta.", Emotion.Preocupado)
             .Enter(ctx => ctx.AddEvidence("ev_zumbido"))
             .Choice("¿Zumbó? ¿Igual que hace dos inviernos?", "n3")
             .Choice("No oí nada. Dormí como un tronco.", "n3b");
            b.Say("n3b", Gina, "Eso es por la túnica. Te dije que daba suerte: hasta el sueño te protege.", Emotion.Alegre).Next("n3");
            b.Say("n3", Gina, "Igual. Un zumbido grave, como alguien tarareando bajo el agua. Eira decía que las Cápsulas no hacen ruido sin motivo.", Emotion.Triste);
            b.Say("n4", Gina, "El anciano Oren dirá que no es nada, que la hondonada está sellada por una buena razón. Pregúntale tú; a mí ya no me cuenta las cosas.")
             .Choice("¿Tú qué crees que les pasó a mamá y a papá?", "n5")
             .Choice("¿Por qué me regalaste esta túnica?", "tunica")
             .Choice("Iré a ver a Oren.", "fin");
            b.Say("n5", Gina, "Creo que se fueron buscando una respuesta, y que la respuesta era más grande de lo que esperaban. Daren nunca se habría ido sin despedirse... salvo si pensaba volver enseguida.", Emotion.Triste);
            b.Say("n5b", Gina, "Pero yo solo sé lo que vi, Nilo. Y lo que uno ve no siempre es toda la historia.")
             .Choice("¿Por qué me regalaste esta túnica?", "tunica")
             .Choice("Iré a ver a Oren.", "fin");
            b.Say("tunica", Gina, "Porque el morado se ve desde lejos. Si algún día te pierdes, quiero que alguien te encuentre. Y porque da suerte, claro. Lo dice tu abuela, así que es verdad.", Emotion.Alegre).Next("fin");
            b.Say("fin", Gina, "Anda, ve. Y come algo por el camino: hay pan de raíz en tu bolsa.", Emotion.Alegre)
             .Enter(ctx =>
             {
                 ctx.SetFlag("gina_intro_done");
                 ctx.AddNotebook("cul_capsulas", Knowledge.Sospecha);
                 ctx.Command("tutorial:basico");
             }).End();
            Reg(b.Build());

            b = new DialogueBuilder("gina_capsula");
            b.Say("start", Gina, "¿Has hablado con Oren? ¿O ya has ido a la hondonada? Tienes la misma cara que ponía tu madre cuando encontraba algo.", Emotion.Curioso);
            GinaTopics(b);
            b.Say("mas", Gina, "¿Algo más, cariño?");
            GinaTopics(b);
            b.Say("pagina", Gina, "Su letra... siempre escribía como si las ideas se le escaparan. «La Cápsula no guarda el pasado: guarda lo que alguien quiso conservar». Muy de Eira: una frase que te deja pensando una semana.", Emotion.Triste)
             .Enter(ctx => { ctx.SetFlag("gina_vio_pagina"); ctx.ChangeAffinity("gina", 5, "compartio_pruebas"); }).Next("mas");
            b.Say("gubia", Gina, "¿Su gubia? Daren no daba un paso sin ella. Si la dejó, es que pensaba volver a por ella enseguida. O que alguien no le dejó cogerla.", Emotion.Sorprendido)
             .Enter(ctx => { ctx.SetFlag("gina_vio_gubia"); ctx.AddEvidence("ev_gina_gubia"); ctx.ChangeAffinity("gina", 3, "compartio_pruebas"); }).Next("mas");
            b.Say("liga", Gina, "¿La Liga? Ese nombre no se pronuncia aquí desde que yo era niña. Los mayores decían que la Liga guarda los recuerdos de todo Orom... y que decide cuáles se olvidan.", Emotion.Preocupado)
             .Enter(ctx => { ctx.SetFlag("gina_liga"); ctx.AddNotebook("cul_liga", Knowledge.Observado); }).Next("mas");
            b.Say("vendas", Gina, "Toma, dos vendas. Y no me mires así: una abuela siempre tiene vendas.", Emotion.Alegre)
             .Enter(ctx => { ctx.GiveItem("venda", 2); MarkDaily(ctx, "gina_vendas_dia"); }).Next("mas");
            b.Say("adios", Gina, "Ve con cuidado. Y si la Cápsula te dice algo, recuerda que las Cápsulas no siempre dicen la verdad.").End();
            Reg(b.Build());

            b = new DialogueBuilder("gina_despedida");
            b.Say("start", Gina, "Así que es verdad. Haro me ha dicho que te ha dado una espada. Una espada, a mi nieto.", Emotion.Triste)
             .Enter(ctx => ctx.Command("encuadre:gina"));
            b.Say("d2", Gina, "No voy a pedirte que te quedes. Tu madre tampoco me escuchó, y tu padre... tu padre la siguió porque la quería. Eres hijo de los dos.", Emotion.Triste)
             .Choice("Volveré, abuela. Con ellos o con respuestas.", "d_a")
             .Choice("Tengo miedo.", "d_b")
             .Choice("¿Por qué nunca me hablaste de la Liga?", "d_c", s => s.Knows("cul_liga"));
            b.Say("d_a", Gina, "Con respuestas basta. Las respuestas también se pueden abrazar, si sabes cómo.", Emotion.Alegre).Next("regalo");
            b.Say("d_b", Gina, "Bien. El miedo te hará mirar dos veces antes de pisar. Lo que no quiero es que te haga quedarte quieto.", Emotion.Determinado).Next("regalo");
            b.Say("d_c", Gina, "Porque callar era lo que hacíamos todos. La tribu cree que el silencio protege. A veces tiene razón. Hoy no.", Emotion.Triste)
             .Enter(ctx => ctx.ChangeAffinity("gina", 5, "verdad_compartida")).Next("regalo");
            b.Say("regalo", Gina, "Toma. Vendas de lino y un ungüento de raíz: los he preparado yo. Y cuida esa túnica. Mientras la lleves, una parte de casa irá contigo.", Emotion.Alegre)
             .Enter(ctx => { ctx.GiveItem("venda", 3); ctx.GiveItem("unguento", 1); });
            b.Say("final", Gina, "Cuando cruces el puente, no mires atrás demasiado rato. Mira hacia delante... pero acuérdate de volver.", Emotion.Triste)
             .Enter(ctx =>
             {
                 ctx.SetFlag("gina_despedida");
                 ctx.ChangeAffinity("gina", 10, "despedida");
                 ctx.Command("despedida_gina");
             }).End();
            Reg(b.Build());

            b = new DialogueBuilder("gina_regreso");
            b.Say("start", Gina, "¡Nilo! Has vuelto. Deja que te mire... estás entero. Más o menos.", Emotion.Sorprendido)
             .Choice("Abuela, tengo algo que contarte. Lo vi en una Cápsula.", "compartir", s => s.Has("dec_compartir"))
             .Choice("Solo quería verte.", "visto");
            b.Say("compartir", Gina, "Cuéntamelo despacio.", Emotion.Preocupado);
            b.Say("compartir2", "Nilo", "Se fueron porque quisieron, abuela. La Liga los esperaba, pero eligieron ir. Y papá dejó escrito que volverían por mí.");
            b.Say("compartir3", Gina, "Así que eligieron... Gracias por no protegerme de la verdad, Nilo. Llevo dos inviernos protegiéndome yo sola, y pesa mucho.", Emotion.Triste)
             .Enter(ctx =>
             {
                 ctx.SetFlag("gina_regreso_visto");
                 ctx.SetFlag("tribu_conoce_recuerdo");
                 ctx.ChangeAffinity("gina", 10, "compartio_verdad");
                 ctx.Notify("Consecuencia comunitaria: la tribu empezará a hablar de su pasado.");
             }).End();
            b.Say("visto", Gina, "Tienes los ojos de alguien que ha visto cosas. No hace falta que me lo cuentes todo... todavía.")
             .Enter(ctx => ctx.SetFlag("gina_regreso_visto")).End();
            Reg(b.Build());

            b = new DialogueBuilder("gina_general");
            b.Say("start", Gina, "¿Necesitas algo, cariño?", Emotion.Alegre);
            GinaGeneralTopics(b);
            b.Say("mas", Gina, "¿Algo más?");
            GinaGeneralTopics(b);
            b.Say("vendas", Gina, "Toma, dos vendas. Una abuela siempre tiene vendas.", Emotion.Alegre)
             .Enter(ctx => { ctx.GiveItem("venda", 2); MarkDaily(ctx, "gina_vendas_dia"); }).Next("mas");
            b.Say("joven", Gina, "Mi abuela decía que caminábamos de noche para que el cielo no nos viera. Nunca entendí qué quería decir. Yo solo recuerdo el frío... y que mi madre cantaba para que no tuviéramos miedo.")
             .Enter(ctx => ctx.AddNotebook("cul_migracion", Knowledge.Observado)).Next("mas");
            b.Say("ojos", Gina, "Tus ojos se parecen cada día más a los de tu madre. Azules como el agua de las calas que ella dibujaba sin haberlas visto nunca.", Emotion.Alegre).End();
            Reg(b.Build());

            // ================================================= OREN
            b = new DialogueBuilder("oren_capsula");
            b.Say("start", Oren, "Nilo. Vienes por el zumbido. Lo sé porque todos vienen por el zumbido y nadie se atreve a preguntar.");
            b.Say("o2", Oren, "Escucha bien: nuestros antepasados caminaron durante generaciones para alejarse de las Cápsulas. Se detuvieron aquí porque el cielo dejó de hablarles. La hondonada se selló para que siguiera callado.")
             .Enter(ctx => ctx.AddNotebook("cul_migracion", Knowledge.Observado))
             .Choice("¿El cielo les hablaba? ¿Qué significa eso?", "cielo")
             .Choice("Mis padres estudiaban esa Cápsula.", "padres");
            b.Say("cielo", Oren, "Es lo que dice la canción. Las canciones no explican, Nilo: recuerdan. Quizá los sueños de unos se metían en la cabeza de otros. Quizá solo era una forma bonita de decir «tuvimos miedo».", Emotion.Curioso)
             .Enter(ctx => ctx.AddNotebook("cul_gran_vigilia", Knowledge.Sospecha)).Next("decide");
            b.Say("padres", Oren, "Y yo les advertí. Eira tenía la cabeza llena de preguntas y Daren el corazón lleno de Eira. Una mezcla peligrosa.", Emotion.Triste).Next("decide");
            b.Say("decide", Oren, "No te prohibiré ir. Ya no eres un niño, y el silencio no ha impedido que la Cápsula vuelva a zumbar. Pero escucha con cuidado: las Cápsulas no dicen la verdad. Dicen lo que guardan.", Emotion.Determinado);
            b.Say("fin", Oren, "Si vas, ve con los ojos abiertos. Y vuelve a contarme lo que veas.")
             .Enter(ctx =>
             {
                 ctx.SetFlag("oren_capsula_done");
                 ctx.AddNotebook("pj_oren", Knowledge.Observado);
                 ctx.AddNotebook("loc_hondonada", Knowledge.Sospecha);
             }).End();
            Reg(b.Build());

            b = new DialogueBuilder("oren_sello");
            b.Say("start", "Nilo", "Oren, alguien abrió el sello de la Cápsula desde fuera... y alguien lo volvió a cerrar. Con nudos nuevos.");
            b.Say("o2", Oren, "...Sí. Fui yo quien lo volvió a cerrar. La mañana en que tus padres se fueron, la hondonada estaba abierta y la Cápsula cantaba.", Emotion.Sorprendido);
            b.Say("o3", Oren, "Até las cuerdas otra vez, colgué los amuletos y no se lo dije a nadie. Ni siquiera a Gina. Pensé que, si nadie hablaba de ello, el mundo de fuera no nos encontraría.", Emotion.Triste)
             .Enter(ctx => ctx.AddEvidence("ev_oren_resello"))
             .Choice("Les ocultaste la verdad a todos.", "reproche")
             .Choice("Entiendo que tuvieras miedo.", "comprension");
            b.Say("reproche", Oren, "Les oculté una pregunta, Nilo. La verdad no la tenía. Pero sí... es lo mismo que hicieron nuestros antepasados. Supongo que el silencio también se hereda.", Emotion.Triste)
             .Enter(ctx => ctx.ChangeAffinity("oren", -5, "le_reprocho")).Next("fin");
            b.Say("comprension", Oren, "Gracias. El miedo es un mal consejero, pero un buen guardián. Hasta que deja de serlo.")
             .Enter(ctx => ctx.ChangeAffinity("oren", 8, "comprension")).Next("fin");
            b.Say("fin", Oren, "Ve. Haz lo que nosotros no hicimos: pregunta.")
             .Enter(ctx => ctx.SetFlag("oren_sello_confrontado")).End();
            Reg(b.Build());

            b = new DialogueBuilder("oren_reaccion");
            b.Say("start", Oren, "Gina me lo ha contado todo. Que nuestros antepasados paraban en aquella Parada, que usaban Cápsulas antes de huir. Que tus padres eligieron ir.");
            b.Say("o2", Oren, "Hay quien está enfadado contigo por hablar. Dicen que has abierto una puerta que costó generaciones cerrar.", Emotion.Preocupado);
            b.Say("o3", Oren, "Yo... creo que ya era hora. El silencio nos protegió. Ahora toca aprender a recordar sin miedo.", Emotion.Determinado)
             .Enter(ctx => { ctx.SetFlag("oren_reaccion"); ctx.ChangeAffinity("oren", 5, "compartio_pruebas"); ctx.AddNotebook("cul_reverso", Knowledge.Sabe); }).End();
            Reg(b.Build());

            b = new DialogueBuilder("oren_general");
            b.Say("start", Oren, "La memoria oral es un río: nunca dice dos veces lo mismo, pero siempre va hacia el mar.");
            OrenTopics(b);
            b.Say("mas", Oren, "¿Algo más, muchacho?");
            OrenTopics(b);
            b.Say("cancion", Oren, "«Caminamos de noche para que el cielo no nos viera; dejamos atrás las casas que recuerdan...» El resto se perdió. O lo perdimos.")
             .Enter(ctx => { ctx.AddNotebook("cul_migracion", Knowledge.Observado); ctx.AddEvidence("ev_piedras_canto"); }).Next("mas");
            b.Say("huecos", Oren, "Porque hubo paradas que decidimos olvidar. No me preguntes cuáles: a mí tampoco me lo dijeron.", Emotion.Triste)
             .Enter(ctx => ctx.AddEvidence("ev_piedras_huecos")).Next("mas");
            b.Say("vigilia", Oren, "Un tiempo en que nadie dormía tranquilo porque los sueños no eran solo suyos. Eso dicen. Yo creo que es una manera de decir que todos tenían miedo a la vez.")
             .Enter(ctx => ctx.AddNotebook("cul_gran_vigilia", Knowledge.Observado)).Next("mas");
            b.Say("adios", Oren, "Que el camino te recuerde.").End();
            Reg(b.Build());

            // ================================================= HARO
            b = new DialogueBuilder("haro_espada");
            b.Say("start", Haro, "Oren dice que vas a cruzar. Gina dice que no. Y yo digo que, si cruzas, cruzarás sabiendo defenderte.", Emotion.Determinado);
            b.Say("h2", Haro, "Esta espada corta era de mi padre. No es bonita, pero no se rompe. Y estos brazales: en los Prados, los ecos muerden los brazos primero.")
             .Enter(ctx =>
             {
                 ctx.GiveItem("espada_reverso", 1);
                 ctx.GiveItem("brazales_cuero", 1);
                 ctx.SetFlag("haro_espada_dada");
                 ctx.AddNotebook("pj_haro", Knowledge.Observado);
             });
            b.Say("h3", Haro, "Equípalos en el inventario ({Inventario}). Luego, al muñeco: golpe rápido con {Atacar}; mantenlo pulsado para un golpe fuerte. Bloquea con {Bloquear}: si lo haces justo a tiempo, desequilibras al rival. Y esquiva con {Esquivar}.");
            b.Say("h4", Haro, "Tres golpes, uno de ellos fuerte, y bloquea el contragolpe del muñeco cuando brille. Cuando lo hagas, hablamos.")
             .Enter(ctx => ctx.Command("tutorial:combate")).End();
            Reg(b.Build());

            b = new DialogueBuilder("haro_entrenar");
            b.Say("start", Haro, "Tres golpes, uno fuerte, y un bloqueo. El muñeco gira cuando le pegas: cuando su brazo brille, bloquea con {Bloquear}. Venga.").End();
            Reg(b.Build());

            b = new DialogueBuilder("haro_espera");
            b.Say("start", Haro, "No está mal. Nada mal. Ahora ve a la puerta del asentamiento: tu abuela te espera allí.");
            b.Say("h2", Haro, "Y, Nilo... vuelve. Gina no sabría a quién regañar.", Emotion.Alegre).End();
            Reg(b.Build());

            b = new DialogueBuilder("haro_bloqueo");
            b.Say("start", Haro, "El puente no es un camino, chico: es una frontera. Antes de pensar en cruzarlo, habla con los tuyos y entiende qué buscas.").End();
            Reg(b.Build());

            b = new DialogueBuilder("haro_general");
            b.Say("start", Haro, "¿Qué necesitas?");
            HaroTopics(b);
            b.Say("mas", Haro, "¿Algo más?");
            HaroTopics(b);
            b.Say("ecos", Haro, "Los ecos atacan en grupo y se lanzan cuando brillan. Bloquea o esquiva, y luego golpea. Y odian el fuego: si te persiguen, busca una hoguera.")
             .Enter(ctx => ctx.AddNotebook("bes_eco", Knowledge.Sospecha)).Next("mas");
            b.Say("jabali", Haro, "Si resoplan y escarban, te están avisando. Aléjate. O dales bayas: son unos glotones.")
             .Enter(ctx => ctx.AddNotebook("bes_jabali", Knowledge.Sospecha)).Next("mas");
            b.Say("adios", Haro, "Mira dónde pisas.").End();
            Reg(b.Build());

            // ================================================= LÍA
            b = new DialogueBuilder("lia_general");
            b.Say("start", Lia, "¡Nilo! ¿Vienes a ayudarme o a pisarme los brotes?", Emotion.Alegre);
            LiaTopics(b);
            b.Say("mas", Lia, "¿Algo más?");
            LiaTopics(b);
            b.Say("juncos", Lia, "En los bordes de los campos, por el lado norte. Y en los Prados, junto al agua, hay muchísimos más. Córtalos con la navaja, que si los arrancas se deshilachan.")
             .Enter(ctx => ctx.AddNotebook("pj_lia", Knowledge.Observado)).Next("mas");
            b.Say("hierba", Lia, "Junto a las piedras con musgo. Dos hojas y un poco de resina, y Tomé te enseña a hacer un ungüento.").Next("mas");
            b.Say("rumor", Lia, "Dicen que en los Prados el viento repite lo que oyó ayer. Mi abuela decía que los Prados «recuerdan». Yo creo que solo hay mucho eco.", Emotion.Curioso).Next("mas");
            b.Say("adios", Lia, "¡Y no pises los brotes al salir!").End();
            Reg(b.Build());

            // ================================================= TOMÉ
            b = new DialogueBuilder("tome_intro");
            b.Say("start", Tome, "¿Quieres aprender a hacer algo útil con las manos, o solo vienes a mirar?")
             .Choice("Quiero aprender.", "ensenar")
             .Choice("Solo miraba.", "mirar");
            b.Say("ensenar", Tome, "Tráeme tres fibras de junco y una resina. Con eso te enseño vendas, ungüentos y, si se te da bien, piezas de cuero.", Emotion.Alegre)
             .Enter(ctx => { ctx.StartQuest("sq_tome"); ctx.AddNotebook("pj_tome", Knowledge.Observado); }).End();
            b.Say("mirar", Tome, "Mirar también es aprender. Pero más despacio.").End();
            Reg(b.Build());

            b = new DialogueBuilder("tome_entrega");
            b.Say("start", Tome, "¿Traes lo que te pedí?")
             .Choice("Aquí tienes: tres fibras y una resina.", "ok", s => s.Inventory.Count("fibra") >= 3 && s.Inventory.Count("resina") >= 1, null, "Necesitas 3 fibras de junco y 1 resina.")
             .Choice("Todavía no.", "no");
            b.Say("ok", Tome, "Mira: así se trenza una venda, así se mezcla un ungüento... y así se corta el cuero para que no tire. La mesa es tuya cuando quieras.", Emotion.Alegre)
             .Enter(ctx =>
             {
                 ctx.TakeItem("fibra", 3);
                 ctx.TakeItem("resina", 1);
                 ctx.SetFlag("receta_basica");
                 ctx.SetFlag("receta_armadura");
                 ctx.GiveItem("venda", 2);
                 ctx.Notify("Recetas aprendidas: ungüento, colgante de fragmento y piezas de explorador.");
             }).End();
            b.Say("no", Tome, "Los juncos no se cortan solos.").End();
            Reg(b.Build());

            b = new DialogueBuilder("tome_general");
            b.Say("start", Tome, "¿Usamos la mesa?");
            TomeTopics(b);
            b.Say("mas", Tome, "¿Algo más?");
            TomeTopics(b);
            b.Say("mesa", Tome, "Adelante. Elige con cabeza: una mejora no se puede deshacer.")
             .Enter(ctx => ctx.Command("open_crafting:mesa")).End();
            b.Say("plegables", Tome, "Mi abuelo juraba que, cuando la tribu caminaba, las casas se plegaban como hojas. Las vigas del almacén todavía tienen bisagras que no sirven para nada... o eso creemos.")
             .Enter(ctx => ctx.AddNotebook("cul_migracion", Knowledge.Observado)).Next("mas");
            b.Say("adios", Tome, "Si algo se rompe, ya sabes dónde estoy.").End();
            Reg(b.Build());

            // ================================================= SURI
            b = new DialogueBuilder("suri_general");
            b.Say("start", Suri, "¿Hambre? Siempre hay hambre.", Emotion.Alegre);
            SuriTopics(b);
            b.Say("mas", Suri, "¿Algo más?");
            SuriTopics(b);
            b.Say("trueque", Suri, "Claro. Enséñame lo que traes.").Enter(ctx => ctx.Command("open_barter:suri")).End();
            b.Say("estofado", Suri, "Toma, recién hecho. Y que no se entere Kiko de que te he servido primero.", Emotion.Alegre)
             .Enter(ctx => { ctx.GiveItem("estofado", 1); MarkDaily(ctx, "suri_estofado_dia"); ctx.AddNotebook("pj_suri", Knowledge.Observado); }).Next("mas");
            b.Say("secreto", Suri, "Doce generaciones de cocineras... y una raíz que solo crece donde paramos por última vez. Mi bisabuela decía que la trajimos de muy lejos.")
             .Enter(ctx => ctx.AddNotebook("cul_migracion", Knowledge.Observado)).Next("mas");
            b.Say("adios", Suri, "Vuelve con hambre.").End();
            Reg(b.Build());

            // ================================================= KIKO
            b = new DialogueBuilder("kiko_general");
            b.Say("start", Kiko, "¡Nilo! ¡Nilo! ¿Sabes guardar un secreto?", Emotion.Alegre)
             .Choice("¿Qué secreto?", "secreto")
             .Choice("¿Me echas una carrera?", "carrera")
             .Choice("Le he puesto nombre a una nube de los Prados.", "nube", s => s.Has("nube_nombrada"))
             .Choice("Ahora no, Kiko.", "adios");
            b.Say("secreto", Kiko, "Hay una cueva detrás de las enredaderas, al oeste, cerca del borde. Yo no quepo... bueno, sí quepo, pero me da miedo. ¡Tienes que agacharte mucho! Y las enredaderas pican: necesitas algo para cortarlas.", Emotion.Curioso)
             .Enter(ctx => { ctx.AddNotebook("loc_gruta", Knowledge.Sospecha); ctx.AddNotebook("pj_kiko", Knowledge.Observado); }).End();
            b.Say("carrera", Kiko, "¡Hasta el Árbol de los Mayores! ¡Ya! ...¡Gané! Bueno, aún no hemos empezado. ¡Pero gané!", Emotion.Alegre).End();
            b.Say("nube", Kiko, "¿¡A una nube!? ¿Y se puede? ¡Cuando sea mayor yo también quiero nombrar nubes!", Emotion.Sorprendido).End();
            b.Say("adios", Kiko, "¡Vale! ¡Pero luego sí!").End();
            Reg(b.Build());
        }

        private static void GinaTopics(DialogueBuilder b)
        {
            b.Choice("Encontré una página del cuaderno de mamá.", "pagina", s => s.HasItem("pagina_eira") && !s.Has("gina_vio_pagina"))
             .Choice("La gubia de papá estaba en el taller.", "gubia", s => s.HasItem("gubia_daren") && !s.Has("gina_vio_gubia"))
             .Choice("Oí una voz en la Cápsula: decía que «la Liga lo había decidido».", "liga", s => s.Heard("res_recuerdo_custodio") && !s.Has("gina_liga"))
             .Choice("¿Me das unas vendas?", "vendas", s => Daily(s, "gina_vendas_dia"))
             .Choice("Todavía nada. Seguiré buscando.", "adios");
        }

        private static void GinaGeneralTopics(DialogueBuilder b)
        {
            b.Choice("¿Me das unas vendas?", "vendas", s => Daily(s, "gina_vendas_dia"))
             .Choice("Cuéntame algo de cuando eras joven.", "joven")
             .Choice("Nada, solo quería verte.", "ojos");
        }

        private static void OrenTopics(DialogueBuilder b)
        {
            b.Choice("Cuéntame la canción de la migración.", "cancion")
             .Choice("¿Por qué faltan piedras en el círculo?", "huecos")
             .Choice("¿Qué fue la Gran Vigilia?", "vigilia", s => s.Knows("cul_gran_vigilia"))
             .Choice("Nada, gracias.", "adios");
        }

        private static void HaroTopics(DialogueBuilder b)
        {
            b.Choice("¿Algún consejo sobre los ecos?", "ecos")
             .Choice("¿Y los jabalíes de musgo?", "jabali")
             .Choice("Nada. Sigo mi camino.", "adios");
        }

        private static void LiaTopics(DialogueBuilder b)
        {
            b.Choice("¿Dónde crecen los juncos?", "juncos")
             .Choice("¿Y la hierba de sanación?", "hierba")
             .Choice("¿Has oído algo raro últimamente?", "rumor")
             .Choice("Nada, sigue con lo tuyo.", "adios");
        }

        private static void TomeTopics(DialogueBuilder b)
        {
            b.Choice("Sí, quiero fabricar o mejorar algo.", "mesa", s => s.Has("receta_basica"), null, "Primero tienes que aprender (misión «El taller de Tomé»).")
             .Choice("Háblame de las casas plegables.", "plegables")
             .Choice("Nada, gracias.", "adios");
        }

        private static void SuriTopics(DialogueBuilder b)
        {
            b.Choice("¿Hacemos un trueque?", "trueque")
             .Choice("Un poco de estofado, por favor.", "estofado", s => Daily(s, "suri_estofado_dia"))
             .Choice("¿Cuál es el secreto del estofado?", "secreto")
             .Choice("Nada, gracias.", "adios");
        }
    }
}
