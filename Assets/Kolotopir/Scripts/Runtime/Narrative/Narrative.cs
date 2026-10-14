using System;
using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Contexto de juego en ejecución: aplica los efectos del contenido narrativo y los hace visibles
    /// (notificaciones, sonidos, cámara, música) además de ejecutar los comandos del guion.
    /// </summary>
    public sealed class RuntimeGameContext : CoreGameContext
    {
        public RuntimeGameContext(GameState state) : base(state)
        {
            state.XpGained += (amount, reason) => Game.UI?.XpGained(amount, reason);
            state.LeveledUp += lvl =>
            {
                Game.UI?.LevelUp(lvl);
                Game.Audio?.Play2D("nivel", 0.8f);
            };
            state.Quests.Started += q => Game.Audio?.Play2D("mision", 0.6f);
            state.Quests.StepCompleted += (q, step) =>
            {
                Game.UI?.ObjectiveCompleted(q, step);
                Game.Audio?.Play2D("ui_abrir", 0.5f, 1.3f);
            };
            state.Quests.Completed += q =>
            {
                Game.UI?.Toast("Misión completada: " + q.Title);
                Game.Audio?.Play2D("mision", 0.8f, 1.15f);
            };
            state.Notebook.EvidenceAdded += _ => Game.Audio?.Play2D("descubrir", 0.5f);
        }

        public override void SetFlag(string flag) { base.SetFlag(flag); Game.MarkDirty(); }
        public override void ClearFlag(string flag) { base.ClearFlag(flag); Game.MarkDirty(); }

        public override void GiveItem(string itemId, int count = 1)
        {
            base.GiveItem(itemId, count);
            Game.Audio?.Play2D("recoger", 0.6f);
            Game.MarkDirty();
        }

        public override bool TakeItem(string itemId, int count = 1)
        {
            bool ok = base.TakeItem(itemId, count);
            if (ok)
            {
                var def = GameDb.Item(itemId);
                if (def != null) Notify(count > 1 ? $"Entregado: {def.Name} ×{count}" : $"Entregado: {def.Name}");
                Game.MarkDirty();
            }
            return ok;
        }

        public override void AddXp(int amount, string reason) { base.AddXp(amount, reason); Game.MarkDirty(); }
        public override void StartQuest(string questId) { base.StartQuest(questId); Game.MarkDirty(); }

        public override void AddNotebook(string entryId, Knowledge knowledge = Knowledge.Observado)
        {
            base.AddNotebook(entryId, knowledge);
            Game.MarkDirty();
        }

        public override void AddEvidence(string evidenceId) { base.AddEvidence(evidenceId); Game.MarkDirty(); }

        public override void ChangeAffinity(string npcId, int delta, string memory = null)
        {
            base.ChangeAffinity(npcId, delta, memory);
            var npc = GameDb.Npc(npcId);
            if (npc != null && !string.IsNullOrEmpty(memory) && delta != 0)
                Game.UI?.Toast(npc.Name + (delta > 0 ? " recordará esto con cariño." : " recordará esto."));
            Game.MarkDirty();
        }

        public override void SetCapsuleState(string capsuleId, CapsuleState state)
        {
            base.SetCapsuleState(capsuleId, state);
            Game.MarkDirty();
        }

        public override void RecordDecision(string id, string option, string summary, ConsequenceScale scale)
        {
            base.RecordDecision(id, option, summary, scale);
            Game.UI?.Toast("Decisión registrada · alcance " + Names.Scale(scale).ToLowerInvariant());
            Game.MarkDirty();
        }

        public override void Notify(string text)
        {
            base.Notify(text);
            if (Log.Count > 200) Log.RemoveRange(0, 100);
            Game.UI?.Toast(Tokens.Apply(text));
        }

        public override void Command(string command)
        {
            base.Command(command);
            if (string.IsNullOrEmpty(command)) return;
            string arg = null;
            int colon = command.IndexOf(':');
            string name = command;
            if (colon > 0) { name = command.Substring(0, colon); arg = command.Substring(colon + 1); }
            switch (name)
            {
                case "autosave":
                    Game.Root?.Autosave("Hito");
                    break;
                case "chapter":
                    if (int.TryParse(arg, out var ch) && ch > State.Chapter)
                    {
                        State.Chapter = ch;
                        Game.UI?.ChapterTitle(ch == 2 ? "Capítulo II" : "Capítulo " + ch, ch == 2 ? "Prados que Recuerdan" : "");
                    }
                    break;
                case "encuadre":
                    Game.Dialogue?.Frame(arg);
                    break;
                case "despedida_gina":
                    Game.Audio?.PlayMusic("musica_despedida", false, 0.7f);
                    break;
                case "revelacion_parada":
                    Game.Audio?.PlayMusic("musica_revelacion", false, 0.8f);
                    Game.Sky?.Pulse(Pal.Resonance, 3f);
                    break;
                case "guardian_escucha":
                    Game.Audio?.Play2D("res_fragmento", 0.8f, 0.7f);
                    Game.Cam?.Shake(0.25f);
                    break;
                case "guardian_calmado":
                    Game.Audio?.PlayMusic("musica_revelacion", false, 0.6f);
                    Game.Sky?.Pulse(Pal.Echo, 2.5f);
                    break;
                case "miga_aparece":
                    SetFlag("miga_aparece");
                    Game.Quests?.Tutorial("miga");
                    break;
                case "open_barter":
                    Game.Dialogue?.AfterClose(() => Game.UI?.OpenBarter(arg));
                    break;
                case "open_crafting":
                    Game.Dialogue?.AfterClose(() => Game.UI?.OpenCrafting(true));
                    break;
                case "tutorial":
                    Game.Quests?.Tutorial(arg);
                    break;
                case "end_chapter_2":
                    Game.Root?.EndChapter();
                    break;
                default:
                    Debug.Log("KOLOTOPIR: comando sin manejar: " + command);
                    break;
            }
        }
    }

    /// <summary>
    /// Diálogos contextuales: el texto depende de la relación, misiones, decisiones y conocimiento (doc 9).
    /// Encuadra al personaje que habla, muestra subtítulos con revelado progresivo y opciones (ratón o teclas 1–4).
    /// </summary>
    public sealed class DialogueController : MonoBehaviour
    {
        public readonly DialogueRunner Runner = new DialogueRunner();
        public NpcAgent Npc { get; private set; }
        public bool Active { get; private set; }
        public float Reveal { get; private set; }
        public bool FullyRevealed => Runner.Current == null || Reveal >= _len;
        public DialogueNode Current => Runner.Current;
        public string CurrentText => Runner.Current != null ? Tokens.Apply(Runner.Current.Text) : "";

        private readonly List<Action> _afterClose = new List<Action>();
        private DialogueNode _shown;
        private float _openedAt;
        private bool _shot;
        private int _len;

        public bool SpeakerIs(NpcAgent agent) => Active && Runner.Current != null && agent != null && Runner.Current.Speaker == agent.Def.Name && !FullyRevealed;

        public void AfterClose(Action a) { if (a != null) _afterClose.Add(a); }

        public void StartWith(NpcAgent npc)
        {
            if (Active || npc == null) return;
            var s = Game.State;
            string id = npc.Def.SelectDialogue != null ? npc.Def.SelectDialogue(s) : null;
            var def = GameDb.Dialogue(id);
            if (!string.IsNullOrEmpty(npc.Def.NotebookEntry)) Game.Ctx.AddNotebook(npc.Def.NotebookEntry, Knowledge.Observado);
            if (def == null)
            {
                npc.SayLine(npc.Def.Species == Species.Kolotapir ? "Hmmf. Hoy no tengo mucho que contar." : "Hoy no hay mucho que contar, Nilo.", 2.5f);
                return;
            }
            if (npc.Sleeping) npc.SayLine("¿Mmh...? ¿Nilo...? Es tardísimo...", 2f);
            Npc = npc;
            Active = true;
            npc.InDialogue = true;
            _openedAt = Time.unscaledTime;
            Game.Cam?.SetFocus(npc.HeadTransform);
            Game.Audio?.Play2D("ui_abrir", 0.4f);
            if (!Runner.Start(def, Game.Ctx)) { Close(); return; }
            OnNode();
            Game.UI?.ShowDialogue(true);
        }

        /// <summary>Encuadre cinematográfico sobre un personaje (comando «encuadre:id»).</summary>
        public void Frame(string npcId)
        {
            var agent = Game.Npcs != null ? Game.Npcs.Find(npcId) : null;
            if (agent == null || Game.Cam == null) return;
            if (Game.Settings != null && Game.Settings.Data.ReduceMotion) { Game.Cam.SetFocus(agent.HeadTransform); return; }
            var head = agent.HeadTransform != null ? agent.HeadTransform.position : agent.transform.position + Vector3.up * 1.6f;
            var player = Game.Player.transform.position;
            var toPlayer = player - agent.transform.position; toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.01f) toPlayer = agent.transform.forward;
            toPlayer.Normalize();
            var right = Vector3.Cross(Vector3.up, toPlayer);
            var camPos = head + toPlayer * 1.7f + right * 0.55f + Vector3.up * 0.05f;
            Game.Cam.BeginShot(camPos, head);
            _shot = true;
        }

        private void OnNode()
        {
            var n = Runner.Current;
            if (n == null) return;
            if (_shown == n) return;
            _shown = n;
            Reveal = 0f;
            _len = CurrentText.Length;
            var speaker = Game.Npcs != null ? FindSpeaker(n.Speaker) : null;
            if (speaker != null)
            {
                speaker.Emotion = n.Emotion;
                if (speaker != Npc && !_shot) Game.Cam?.SetFocus(speaker.HeadTransform);
                else if (!_shot && Npc != null) Game.Cam?.SetFocus(Npc.HeadTransform);
                var def = speaker.Def;
                Game.Audio?.Voice(def.Species, def.VoicePitch, Mathf.Clamp(n.Text.Length / 12, 2, 8), speaker.transform.position + Vector3.up * 1.5f);
            }
            Game.UI?.DialogueNodeChanged();
        }

        private NpcAgent FindSpeaker(string name)
        {
            if (Npc != null && Npc.Def.Name == name) return Npc;
            foreach (var a in Game.Npcs.Agents) if (a.Def.Name == name) return a;
            return null;
        }

        private void Update()
        {
            if (!Active) return;
            if (Runner.Finished) { Close(); return; }
            const float cps = 55f;
            Reveal = Mathf.Min(_len, Reveal + cps * Time.unscaledDeltaTime);
            if (Npc != null && Vector3.Distance(Npc.transform.position, Game.Player.transform.position) > 9f) { Close(); return; }
            if (Game.UI != null && Game.UI.ModalOpen) return;
            if (Time.unscaledTime - _openedAt < 0.25f) return;

            var choices = Runner.VisibleChoices();
            bool advance = InputMap.Down(InputAction.Interactuar) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0) && !Game.UI.PointerOverButton();
            if (!FullyRevealed)
            {
                if (advance) Reveal = _len;
                return;
            }
            if (choices.Count == 0)
            {
                if (advance) Advance();
            }
            else
            {
                for (int i = 0; i < choices.Count && i < 9; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) { Choose(choices[i]); return; }
            }
            if (InputMap.Down(InputAction.Pausa) || Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        public void Advance()
        {
            if (!Active) return;
            if (!FullyRevealed) { Reveal = _len; return; }
            Game.Audio?.Play2D("ui_clic", 0.3f);
            Runner.Continue();
            if (Runner.Finished) Close();
            else OnNode();
        }

        public void Choose(DialogueChoice c)
        {
            if (!Active || c == null) return;
            if (Runner.IsLocked(c))
            {
                if (!string.IsNullOrEmpty(c.LockedReason)) Game.UI?.Toast(c.LockedReason);
                Game.Audio?.Play2D("aviso", 0.4f);
                return;
            }
            Game.Audio?.Play2D("ui_clic", 0.4f);
            Runner.Choose(c);
            Game.MarkDirty();
            if (Runner.Finished) Close();
            else OnNode();
        }

        public void Close()
        {
            if (!Active) return;
            Active = false;
            Runner.Stop();
            _shown = null;
            if (Npc != null) { Npc.InDialogue = false; Npc.Emotion = Emotion.Neutral; }
            Npc = null;
            if (_shot) { Game.Cam?.EndShot(); _shot = false; }
            Game.Cam?.ClearFocus();
            Game.UI?.ShowDialogue(false);
            Game.Audio?.Play2D("ui_cerrar", 0.3f);
            Game.MarkDirty();
            var pending = new List<Action>(_afterClose);
            _afterClose.Clear();
            foreach (var a in pending) a();
        }
    }

    /// <summary>
    /// Seguimiento de misiones: reevalúa las condiciones, guarda en hitos, ofrece pistas graduales
    /// y muestra tutoriales contextuales (doc: "tutorial integrado en la historia").
    /// </summary>
    public sealed class QuestTracker : MonoBehaviour
    {
        private bool _dirty = true;
        private float _timer;
        private readonly HashSet<string> _tutorialsShown = new HashSet<string>();
        private readonly Queue<string> _tutorialQueue = new Queue<string>();
        private float _tutorialTimer;
        public string TrackedQuest;

        public void MarkDirty() => _dirty = true;

        public void ResetSession()
        {
            _tutorialsShown.Clear();
            _tutorialQueue.Clear();
            TrackedQuest = null;
            _dirty = true;
        }

        private void LateUpdate()
        {
            var s = Game.State;
            if (s == null || !Game.InGame) return;
            _timer -= Time.deltaTime;
            if (_dirty || _timer <= 0f)
            {
                _dirty = false;
                _timer = 0.3f;
                bool mainBefore = MainProgressKey(s, out var before);
                if (s.Quests.Evaluate(s, Game.Ctx))
                {
                    MainProgressKey(s, out var after);
                    if (mainBefore && before != after) Game.Root?.Autosave("Hito de la historia");
                    _dirty = true;
                }
            }

            if (InputMap.Down(InputAction.Pista) && !(Game.UI != null && Game.UI.BlocksGameplay)) Game.Miga?.RequestHint();

            if (_tutorialQueue.Count > 0)
            {
                _tutorialTimer -= Time.unscaledDeltaTime;
                if (_tutorialTimer <= 0f && (Game.Dialogue == null || !Game.Dialogue.Active))
                {
                    Game.UI?.Tutorial(Tokens.Apply(_tutorialQueue.Dequeue()));
                    _tutorialTimer = 6.5f;
                }
            }
        }

        private static bool MainProgressKey(GameState s, out string key)
        {
            key = "";
            foreach (var p in s.Quests.All)
            {
                var def = GameDb.Quest(p.Id);
                if (def != null && def.Kind == QuestKind.Principal) key += p.Id + p.Step + (int)p.Status + ";";
            }
            return true;
        }

        /// <summary>Misión seguida: la elegida en el cuaderno, o la principal activa más reciente.</summary>
        public QuestDef Tracked()
        {
            var s = Game.State;
            if (s == null) return null;
            if (!string.IsNullOrEmpty(TrackedQuest) && s.Quests.IsActive(TrackedQuest)) return GameDb.Quest(TrackedQuest);
            QuestDef best = null;
            int bestOrder = -1;
            foreach (var p in s.Quests.All)
            {
                if (p.Status != QuestStatus.Activa) continue;
                var def = GameDb.Quest(p.Id);
                if (def == null) continue;
                int order = p.Order + (def.Kind == QuestKind.Principal ? 100000 : 0);
                if (order > bestOrder) { bestOrder = order; best = def; }
            }
            return best;
        }

        public QuestStep HintStep(out QuestDef quest)
        {
            quest = Tracked();
            if (quest == null) return null;
            return Game.State.Quests.CurrentStep(quest.Id);
        }

        public string ObjectiveText()
        {
            var q = Tracked();
            if (q == null) return null;
            var step = Game.State.Quests.CurrentStep(q.Id);
            if (step == null) return null;
            string text = Tokens.Apply(step.Text);
            if (step.Detail != null)
            {
                try { text += "\n<i>" + step.Detail(Game.State) + "</i>"; } catch (Exception) { }
            }
            return text;
        }

        /// <summary>Resuelve "npc:id", "loc:id", "cap:id" o "x,z" a una posición del mundo.</summary>
        public bool TryGetTargetPosition(string target, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (string.IsNullOrEmpty(target)) return false;
            int colon = target.IndexOf(':');
            string kind = colon > 0 ? target.Substring(0, colon) : "";
            string id = colon > 0 ? target.Substring(colon + 1) : target;
            switch (kind)
            {
                case "npc":
                    var a = Game.Npcs != null ? Game.Npcs.Find(id) : null;
                    if (a == null) return false;
                    pos = a.transform.position + Vector3.up * 1.6f;
                    return true;
                case "loc":
                    var l = GameDb.Location(id);
                    if (l == null) return false;
                    pos = K.OnGround(l.Pos, 1f);
                    return true;
                case "cap":
                    pos = K.OnGround(id == "cap_sellada" ? WorldLayout.SealedCapsule : WorldLayout.Ruins, 1.2f);
                    return true;
                default:
                    var parts = id.Split(',');
                    if (parts.Length == 2 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)
                        && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var z))
                    {
                        pos = K.OnGround(x, z, 1f);
                        return true;
                    }
                    return false;
            }
        }

        // ------------------------------------------------------------------ Tutoriales integrados

        public void Tutorial(string id)
        {
            if (string.IsNullOrEmpty(id) || _tutorialsShown.Contains(id)) return;
            if (Game.State != null && Game.State.Has("tut_" + id)) return;
            _tutorialsShown.Add(id);
            Game.State?.Flags.Set("tut_" + id);
            switch (id)
            {
                case "basico":
                    Enqueue("Muévete con {Adelante} {Izquierda} {Atras} {Derecha} y mira con el ratón. Corre con {Correr} y salta con {Saltar}.");
                    Enqueue("Habla e interactúa con {Interactuar}. Tu cuaderno ({Cuaderno}) guarda misiones, personas, lugares y misterios.");
                    Enqueue("¿Atascado? Pide una pista con {Pista}. Cuanto más la pidas para el mismo objetivo, más clara será.");
                    break;
                case "combate":
                    Enqueue("Golpe rápido: {Atacar}. Golpe fuerte: mantén {Atacar}. Encadena tres golpes rápidos para un combo.");
                    Enqueue("Bloquea con {Bloquear}. Un bloqueo justo cuando el enemigo golpea (cuando brilla) lo desequilibra: contraataca.");
                    Enqueue("Esquiva con {Esquivar} en la dirección en la que te muevas. El aguante se gasta al atacar, bloquear y esquivar.");
                    break;
                case "escucha":
                    Enqueue("Mantén {Escuchar} cerca de una fuente de Resonancia para oír lo que recuerda. El anillo violeta indica la intensidad.");
                    Enqueue("Escuchar hace ruido para los ecos: no lo hagas con enemigos cerca.");
                    break;
                case "miga":
                    Enqueue("Miga, una pequeña criatura de papel, ha decidido acompañarte. Con {Pista} te guiará hacia tu objetivo.");
                    break;
                case "inventario":
                    Enqueue("Abre el inventario con {Inventario}. Equipa armas y armaduras y asigna objetos rápidos ({SiguienteObjeto} para cambiar, {UsarObjeto} para usar).");
                    break;
                case "nivel":
                    Enqueue("¡Has subido de nivel! Reparte los puntos de atributo y de habilidad en la pestaña Personaje del inventario ({Inventario}).");
                    break;
                case "noche":
                    Enqueue("Cae la noche. Los ecos se vuelven más activos fuera de los asentamientos. Las hogueras y los faroles los mantienen a raya.");
                    break;
                case "descanso":
                    Enqueue("En los lugares de descanso puedes dormir, pasar el tiempo y guardar la partida manualmente.");
                    break;
                case "catalejo":
                    Enqueue("Mantén {Catalejo} para mirar a lo lejos con el catalejo de Nilo. Rueda del ratón: zoom.");
                    break;
                case "puente":
                    Enqueue("El puente se balancea: mantén el equilibrio con {Izquierda} y {Derecha} cuando se incline.");
                    break;
            }
        }

        private void Enqueue(string text)
        {
            _tutorialQueue.Enqueue(text);
            if (_tutorialQueue.Count == 1) _tutorialTimer = Mathf.Min(_tutorialTimer, 0.5f);
        }
    }
}
