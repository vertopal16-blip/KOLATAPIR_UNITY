using System.Collections.Generic;

namespace Kolotopir.Core
{
    public static partial class GameDb
    {
        private static ResonanceDef R(string id, ResonanceType type, string title, string note, params ResonanceLine[] lines)
        {
            var r = new ResonanceDef { Id = id, Type = type, Title = title, Note = note };
            r.Lines.AddRange(lines);
            Reg(r);
            return r;
        }

        private static ResonanceLine L(string speaker, string text) => new ResonanceLine(speaker, text);
        private static ResonanceLine L(string text) => new ResonanceLine("", text);

        private static bool CanReadDeep(GameState s) => s.Attr(AttributeType.Resonancia) >= 3 || s.HasSkill("lectura_profunda");

        private static void AddResonances()
        {
            // ------------------------------------------------ Capítulo I: la Cápsula sellada y el taller
            var huellas = R("res_huellas_capsula", ResonanceType.Ambiental, "Pasos en la escarcha",
                "Dos rastros salen de aquí hacia el puente. Uno dudó.",
                L("Frío. La escarcha cruje bajo dos pares de pies."),
                L("Se alejan de la Cápsula. Uno se detiene... vuelve sobre sus pasos..."),
                L("...duda un instante y sigue al otro, hacia el puente."));
            huellas.Evidence = new[] { "ev_huellas", "ev_capsula_anomala" };
            huellas.Xp = 30;
            huellas.OnHeard = ctx =>
            {
                ctx.SetFlag("huellas_reveladas");
                ctx.Command("miga_aparece");
                ctx.AddNotebook("cul_capsulas", Knowledge.Observado);
            };

            var custodio = R("res_recuerdo_custodio", ResonanceType.Contradictoria, "Una voz que no encaja",
                "Esto no encaja con lo que oí en el taller... ¿o sí? Puede ser mentira, error, perspectiva o manipulación.",
                L("Voz grave", "Los dos vendrán con nosotros."),
                L("Voz grave", "La Liga lo ha decidido."),
                L("El zumbido se corta de golpe, como una puerta que alguien cierra."));
            custodio.Requirement = s => s.Heard("res_huellas_capsula");
            custodio.RequirementText = "La Cápsula todavía no deja oír nada más.";
            custodio.Evidence = new[] { "ev_recuerdo_custodio" };
            custodio.Xp = 30;
            custodio.OnHeard = ctx => { ctx.AddNotebook("cul_liga", Knowledge.Sospecha); ctx.AddNotebook("pj_custodio_gris", Knowledge.Sospecha); };

            var sello = R("res_sello", ResonanceType.Ambiental, "Nudos nuevos sobre cortes viejos",
                "Alguien mayor volvió a cerrar el sello cantando la canción de la migración.",
                L("Un golpe seco: alguien corta las cuerdas desde fuera."),
                L("Mucho después, otras manos, más viejas, atan nudos nuevos con cuidado."),
                L("Mientras trabajan, murmuran la canción de la migración."));
            sello.Requirement = s => s.Heard("res_huellas_capsula");
            sello.RequirementText = "Primero escucha la Cápsula.";
            sello.Evidence = new[] { "ev_sello_canto" };

            var daren = R("res_recuerdo_daren", ResonanceType.Personal, "Una noche en el taller",
                "Decidieron ir. Nadie les obligó... al menos aquí.",
                L("Daren", "Eira, no tenemos que ir. Aquí estamos a salvo. Nilo está a salvo."),
                L("Eira", "¿A salvo de qué, Daren? ¿De saber?"),
                L("Eira", "Si no vamos, nadie sabrá lo que el Custodio está haciendo con los archivos."),
                L("Daren", "...Entonces iremos juntos. Y volveremos antes del deshielo."));
            daren.Evidence = new[] { "ev_recuerdo_daren" };
            daren.Xp = 30;
            daren.OnHeard = ctx => { ctx.AddNotebook("pj_eira", Knowledge.Observado); ctx.AddNotebook("pj_daren", Knowledge.Observado); };

            var piedras = R("res_piedras", ResonanceType.Ambiental, "Canto de la migración",
                "Hablan de 'casas que recuerdan'. ¿Cápsulas?",
                L("Muchas voces", "Caminamos de noche para que el cielo no nos viera..."),
                L("Muchas voces", "...dejamos atrás las casas que recuerdan, dejamos atrás lo que fuimos."),
                L("Una voz sola sigue cantando, pero alguien la hace callar."));
            piedras.Evidence = new[] { "ev_piedras_canto" };
            piedras.OnHeard = ctx => ctx.AddNotebook("cul_migracion", Knowledge.Observado);

            var arbol = R("res_arbol", ResonanceType.Profunda, "Bajo el Árbol de los Mayores",
                "Hay una semilla de Cápsula bajo el árbol. La tribu no siempre temió a las Cápsulas.",
                L("Una anciana", "Enterradla aquí. Que crezca un árbol encima."),
                L("Un hombre", "¿Y si un día la necesitamos?"),
                L("La anciana", "Entonces que la encuentren los que no tengan miedo. Que nadie recuerde lo que fuimos... hasta que haga falta."));
            arbol.Type = ResonanceType.Profunda;
            arbol.Requirement = CanReadDeep;
            arbol.RequirementText = "Resonancia profunda: necesitas Resonancia 3 o la habilidad Lectura profunda.";
            arbol.Evidence = new[] { "ev_arbol_profundo" };
            arbol.Xp = 60;

            // ------------------------------------------------ Capítulo II: los Prados
            var rastro = R("res_rastro_prados", ResonanceType.Ambiental, "Pasos en la hierba alta",
                "Había alguien más con ellos. Alguien de pasos largos.",
                L("Hierba aplastada por dos pares de pies..."),
                L("...y un tercero, más pesado, de zancada larga."),
                L("Van hacia el norte, hacia el agua."));
            rastro.Evidence = new[] { "ev_tercer_rastro" };
            rastro.Xp = 30;
            rastro.OnHeard = ctx => ctx.SetFlag("huellas_prados");

            var g1 = R("res_guardian_1", ResonanceType.Personal, "Eco del Guardián (I)", "El Guardián hizo una promesa.",
                L("Guardián", "Prometí cuidar la Parada hasta que volvieran los viajeros."));
            g1.OnHeard = ctx => GuardianEchoHeard(ctx);
            var g2 = R("res_guardian_2", ResonanceType.Personal, "Eco del Guardián (II)", "Los viajeros eran del Reverso.",
                L("Guardián", "Los viajeros del Reverso se marcharon una mañana. Dijeron que el cielo hablaba demasiado."),
                L("Guardián", "Me dejaron sus Cápsulas. Y su promesa."));
            g2.Evidence = new[] { "ev_guardian_viajeros" };
            g2.OnHeard = ctx => GuardianEchoHeard(ctx);
            var g3 = R("res_guardian_3", ResonanceType.Personal, "Eco del Guardián (III)", "Ha olvidado su nombre, pero no su promesa.",
                L("Guardián", "¿Cuántos inviernos? Ya no recuerdo mi nombre."),
                L("Guardián", "Solo recuerdo la promesa."));
            g3.OnHeard = ctx => GuardianEchoHeard(ctx);

            var calma = R("res_guardian_calma", ResonanceType.Profunda, "Devolver el recuerdo",
                "La promesa estaba cumplida. Solo necesitaba que alguien se lo dijera.",
                L("Nilo", "Los viajeros no van a volver. Pero yo soy del Reverso. Ya puedes descansar."),
                L("Guardián", "...Del Reverso. Has vuelto."),
                L("Guardián", "Entonces la promesa está cumplida."));
            calma.Requirement = s => s.Has("guardian_ecos_completos");
            calma.RequirementText = "El Guardián no te escucha todavía. Quizá haya algo que deba recordar.";
            calma.Xp = 40;
            calma.OnHeard = ctx =>
            {
                ctx.SetFlag("guardian_resuelto");
                ctx.SetFlag("guardian_calmado");
                ctx.GiveItem("cuenta_guardian", 1);
                ctx.AddNotebook("bes_guardian", Knowledge.Sabe);
                ctx.AddNotebook("pj_guardian", Knowledge.Sabe);
                ctx.RecordDecision("dec_guardian", "Calmado", "Devolviste su recuerdo al Guardián de la Parada en lugar de luchar.", ConsequenceScale.Local);
                ctx.Command("guardian_calmado");
            };

            var parada = R("res_parada_distorsion", ResonanceType.Contradictoria, "Cristal roto",
                "La Cápsula está demasiado dañada. Necesita reparación antes de poder escucharla de verdad.",
                L("«...esperábamos...»"), L("«...iremos...»"), L("«...Nilo...»"),
                L("Las palabras se rompen como cristal."));
            parada.Xp = 10;

            var talla = R("res_talla_refugio", ResonanceType.Material, "La talla del refugio",
                "Volverían por mí. Y si no podían, querían que yo fuera.",
                L("Daren graba con la punta de un cuchillo: «Para Nilo. Volveremos por él»."),
                L("Eira", "¿Y si no nos dejan volver?"),
                L("Daren", "Entonces que lo lea él. Y que venga."));
            talla.Requirement = s => s.Has("recuerdo_reconstruido");
            talla.RequirementText = "Algo grabado en la pared... todavía no sabes leerlo.";
            talla.Evidence = new[] { "ev_talla_volveremos" };
            talla.Xp = 30;

            var risa = R("res_prados_risa", ResonanceType.Ambiental, "Una risa en la hierba",
                "Los Prados recuerdan cosas pequeñas.",
                L("Una risa infantil, de hace mucho. Alguien persigue a alguien entre la hierba."));
            risa.Xp = 12;
            var cancion = R("res_prados_cancion", ResonanceType.Ambiental, "Una canción conocida",
                "Es la melodía de la migración del Reverso... en lengua kolotapir.",
                L("Una canción en lengua kolotapir. No entiendes las palabras."),
                L("Pero conoces la melodía: es la misma que canta Oren junto al fuego."));
            cancion.Evidence = new[] { "ev_cancion_compartida" };
            var nube = R("res_prados_nube", ResonanceType.Ambiental, "Una discusión antigua",
                "Los kolotapires llevan generaciones discutiendo sobre nubes.",
                L("Kolotapir", "¡Es un pez!"), L("Otra kolotapir", "¡Es una tetera!"),
                L("El eco tiene por lo menos cien años."));
            nube.Xp = 12;
            var caravana = R("res_prados_caravana", ResonanceType.Ambiental, "Faroles azules",
                "Una caravana de la Liga. Alguien la llama 'Primer Custodio'.",
                L("Ruedas, muchas. Una caravana con faroles azules avanza hacia el este."),
                L("Voz", "Más deprisa. El Primer Custodio no espera."));
            caravana.Evidence = new[] { "ev_primer_custodio" };

            var mirador = R("res_mirador", ResonanceType.Personal, "Mil maneras de recordar",
                "Mamá estuvo aquí. Miraba la Ciudad.",
                L("Eira", "Mil Cápsulas, Daren. Mil maneras de recordar."),
                L("Eira", "¿Y si una de ellas es la nuestra?"));
            mirador.Xp = 25;

            var boro = R("res_boro_cuenta", ResonanceType.Personal, "La cuenta de Boro",
                "Boro renunció a este recuerdo para proteger a su hermana. Su rostro ya no está.",
                L("Boro, joven", "Mira, Lirio: he encontrado un caracol con la concha al revés."),
                L("Una kolotapir joven", "Boro, siempre encuentras lo que nadie busca."),
                L("Su rostro está borroso. Por mucho que lo intentes, no se aclara."));
            boro.IsPrivate = true;
            boro.OwnerNpc = "boro";
            boro.ConsentFlag = "consent_boro_cuenta";
            boro.Xp = 20;
            boro.OnHeard = ctx =>
            {
                if (ctx.State.Has("consent_boro_cuenta"))
                {
                    ctx.ChangeAffinity("boro", 8, "escucho_con_permiso");
                    ctx.RecordDecision("dec_boro_cuenta", "Escuchada con permiso", "Escuchaste el recuerdo de Boro con su consentimiento.", ConsequenceScale.Local);
                }
                else
                {
                    ctx.SetFlag("boro_cuenta_sin_permiso");
                    ctx.ChangeAffinity("boro", -25, "invadio_privacidad");
                    ctx.RecordDecision("dec_boro_cuenta", "Escuchada sin permiso", "Escuchaste un recuerdo privado de Boro sin su consentimiento. Él lo notará.", ConsequenceScale.Local);
                }
                ctx.AddNotebook("pj_boro", Knowledge.Sabe);
            };
        }

        private static void GuardianEchoHeard(IGameContext ctx)
        {
            var s = ctx.State;
            if (s.Heard("res_guardian_1") && s.Heard("res_guardian_2") && s.Heard("res_guardian_3") && !s.Has("guardian_ecos_completos"))
            {
                ctx.SetFlag("guardian_ecos_completos");
                ctx.Notify("El Guardián se detiene. Parece escuchar algo. Acércate y usa la Escucha frente a él.");
                ctx.Command("guardian_escucha");
            }
        }

        private static void AddCapsules()
        {
            var sellada = new CapsuleDef
            {
                Id = "cap_sellada",
                Name = "Cápsula sellada del Reverso",
                InitialState = CapsuleState.Anomala,
                Description = "Una Cápsula antigua envuelta en cuerdas, amuletos y cintas descoloridas. La tribu la selló hace generaciones.",
                ListenSequence = { "res_huellas_capsula", "res_recuerdo_custodio", "res_sello" },
            };
            sellada.ExamineText[CapsuleState.Anomala] = "Late con una luz violeta irregular, como un corazón que no encuentra su ritmo. El sello de cuerdas está intacto... pero bajo los nudos nuevos hay cortes antiguos, hechos desde fuera.";
            sellada.ExamineText[CapsuleState.Sellada] = "Silencio. La luz se ha apagado del todo y los amuletos cuelgan quietos. Lo que guardaba ya no responde.";
            sellada.ExamineText[CapsuleState.Estable] = "Una luz tranquila, casi cálida. Por primera vez parece una Cápsula y no una amenaza.";
            sellada.OnExamine = ctx =>
            {
                if (!ctx.State.Has("pista_sello"))
                {
                    ctx.SetFlag("pista_sello");
                    ctx.AddEvidence("ev_sello");
                    ctx.Notify("Pista: el sello se abrió desde fuera y alguien volvió a cerrarlo.");
                }
                ctx.AddNotebook("cul_capsulas", Knowledge.Observado);
            };
            Reg(sellada);

            var parada = new CapsuleDef
            {
                Id = "cap_parada",
                Name = "Cápsula de la Parada",
                InitialState = CapsuleState.Danada,
                Description = "Una Cápsula kolotapir agrietada en el centro de la Parada Antigua.",
                ListenSequence = { "res_parada_distorsion" },
                RepairCost = { new ItemAmount("fragmento", 1) },
                RepairAvailable = s => s.Has("guardian_resuelto"),
                RepairFlag = "cap_parada_reparada",
                RepairText = "Encajar un fragmento de cápsula en la grieta",
                PuzzleIntro = "La Cápsula ofrece cuatro fragmentos de un mismo recuerdo, desordenados. Ordénalos para reconstruirlo. Cada fragmento lleva un símbolo grabado.",
                PuzzleHints = new[]
                {
                    "Piensa en cómo empieza y cómo termina un viaje: llegar, encontrarse, decidir, marcharse.",
                    "Boro habló de un orden kolotapir de la memoria. La inscripción del arco de piedra también.",
                    "Agua, raíz, piedra, cielo.",
                },
                PuzzleAvailable = s => s.Has("cap_parada_reparada"),
                PuzzleLockedText = "La grieta deja escapar el recuerdo antes de que puedas sostenerlo. Primero hay que repararla.",
                PuzzleSolvedFlag = "recuerdo_reconstruido",
                DecisionAvailable = s => s.Has("recuerdo_reconstruido"),
                DecisionFlag = "decision_recuerdo",
            };
            parada.ExamineText[CapsuleState.Danada] = "Una Cápsula kolotapir agrietada. Por la grieta escapa una luz que parpadea: emite señales incompletas, distorsionadas o contradictorias. El suelo está cubierto de fragmentos.";
            parada.ExamineText[CapsuleState.Estable] = "La grieta está cerrada con un fragmento. La luz es estable y cálida.";
            parada.ExamineText[CapsuleState.Inactiva] = "La Cápsula está apagada. Solo tú sabes lo que guardaba.";
            parada.ExamineText[CapsuleState.Sellada] = "Has atado la Cápsula con juncos y la has cubierto de piedras. Nadie volverá a escucharla.";
            parada.OnExamine = ctx => ctx.AddNotebook("cul_capsulas", Knowledge.Sabe);
            parada.Fragments.Add(new MemoryFragment { Id = "f_piedra", Symbol = "Piedra", Text = "Eira aprieta la mano de Daren. «Iremos. Pero no porque la Liga lo decida: queremos ver los archivos con nuestros propios ojos.»" });
            parada.Fragments.Add(new MemoryFragment { Id = "f_cielo", Symbol = "Cielo", Text = "Daren graba algo en la pared del refugio. El Custodio mira hacia el este, hacia la Ciudad de las Mil Cápsulas. «En marcha.»" });
            parada.Fragments.Add(new MemoryFragment { Id = "f_agua", Symbol = "Agua", Text = "El estanque refleja dos faroles. Eira se arrodilla junto a una columna: «Aquí paraban nuestros antepasados, Daren. Mira: marcas del Reverso.»" });
            parada.Fragments.Add(new MemoryFragment { Id = "f_raiz", Symbol = "Raíz", Text = "Una figura alta con capa gris sale de entre las raíces del arco. «Os esperábamos. Los dos vendréis con nosotros. La Liga lo ha decidido.»" });
            parada.CorrectOrder = new[] { "f_agua", "f_raiz", "f_piedra", "f_cielo" };
            parada.OnPuzzleSolved = ctx =>
            {
                ctx.SetFlag("recuerdo_reconstruido");
                ctx.SetCapsuleState("cap_parada", CapsuleState.Estable);
                ctx.AddEvidence("ev_marcas_parada");
                ctx.AddEvidence("ev_conexion_parada");
                ctx.AddEvidence("ev_custodio_esperaba");
                ctx.AddEvidence("ev_recuerdo_completo");
                ctx.AddNotebook("pj_eira", Knowledge.Sabe);
                ctx.AddNotebook("pj_daren", Knowledge.Sabe);
                ctx.AddNotebook("pj_custodio_gris", Knowledge.Observado);
                ctx.AddNotebook("cul_migracion", Knowledge.Sabe);
                ctx.AddXp(100, "Recuerdo reconstruido");
                ctx.Command("revelacion_parada");
            };
            parada.Decisions.Add(new CapsuleDecision
            {
                Id = "conservar", Label = "Conservarlo en la Cápsula", Scale = ConsequenceScale.Local, ResultState = CapsuleState.Estable,
                Description = "La Cápsula queda estable. El recuerdo seguirá aquí para quien lo busque... también para la Liga.",
                Effect = ctx => ctx.SetFlag("dec_conservar"),
            });
            parada.Decisions.Add(new CapsuleDecision
            {
                Id = "compartir", Label = "Compartirlo con la tribu", Scale = ConsequenceScale.Comunitaria, ResultState = CapsuleState.Estable,
                Description = "Lo contarás en el Reverso. Gina sabrá que se fueron por voluntad propia; la tribu tendrá que mirar de frente su pasado.",
                Effect = ctx => { ctx.SetFlag("dec_compartir"); ctx.SetFlag("gina_sabe_recuerdo"); },
            });
            parada.Decisions.Add(new CapsuleDecision
            {
                Id = "ocultar", Label = "Ocultarlo", Scale = ConsequenceScale.Local, ResultState = CapsuleState.Inactiva,
                Description = "Lo guardarás solo para ti. Apagas la Cápsula: nadie en la tribu sabrá lo que viste, tampoco sufrirán por ello.",
                Effect = ctx => ctx.SetFlag("dec_ocultar"),
            });
            parada.Decisions.Add(new CapsuleDecision
            {
                Id = "inaccesible", Label = "Dejarlo inaccesible", Scale = ConsequenceScale.Global, ResultState = CapsuleState.Sellada,
                Description = "Sellas la Cápsula para que nadie, ni la Liga, vuelva a escucharlo. La conexión con la Cápsula del Reverso se rompe y los ecos de la Parada se calmarán. El recuerdo se perderá para todos.",
                Effect = ctx =>
                {
                    ctx.SetFlag("dec_inaccesible");
                    ctx.SetCapsuleState("cap_sellada", CapsuleState.Sellada);
                },
            });
            Reg(parada);
        }
    }
}
