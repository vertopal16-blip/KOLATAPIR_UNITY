using System.Collections;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Orquesta la partida: arranque, menú principal, partida nueva y carga, reloj del día,
    /// regiones y descubrimientos, derrota y recuperación, descanso, guardado y final del capítulo II.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        /// <summary>Una hora de juego dura este número de segundos reales.</summary>
        public const float SecondsPerGameHour = 60f;
        private const float AutosaveInterval = 360f;

        public GamePhase Phase { get; private set; } = GamePhase.Booting;
        public PaperDoll Doll { get; private set; }

        private GameObject _playerGo;
        private Camera _menuCam;
        private float _menuOrbit;
        private float _hitStopUntil;
        private float _autosaveTimer = AutosaveInterval;
        private bool _regionKnown;
        private Region _region;
        private float _checkTimer;
        private bool _busy;
        private bool _recovering;
        private Coroutine _flow;

        private static readonly Vector3 MenuFocus = new Vector3(170f, 40f, 175f);

        // ------------------------------------------------------------------ Arranque

        public static void Boot()
        {
            if (Game.Root != null) return;
            var go = new GameObject("KOLOTOPIR");
            go.AddComponent<GameRoot>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Game.Root = null; Game.State = null; Game.Ctx = null; Game.Player = null; Game.Cam = null; Game.Combat = null;
            Game.Vitals = null; Game.Interactor = null; Game.Listener = null; Game.UI = null; Game.Audio = null; Game.World = null;
            Game.Sky = null; Game.Npcs = null; Game.Dialogue = null; Game.Quests = null; Game.Resonance = null; Game.Settings = null; Game.Miga = null;
            CreatureBase.All.Clear();
            Interactable.All.Clear();
            Listenables.All.Clear();
            LightSource.All.Clear();
            FirePoint.Points.Clear();
        }

        private void Awake()
        {
            if (Game.Root != null && Game.Root != this) { Destroy(gameObject); return; }
            Game.Root = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            Phase = GamePhase.Booting;
            Application.runInBackground = true;
            Game.Settings = new GameSettings();
            Game.Settings.Load();
            U.TextScale = Game.Settings.Data.TextScale;
            U.HighContrast = Game.Settings.Data.HighContrast;
            MaterialLibrary.Init();

            Game.UI = UIRoot.Create();
            Game.UI.SetLoading(true, 0.03f, "Despertando Orom…");
            yield return null;

            var audioGo = new GameObject("Sonido");
            audioGo.transform.SetParent(transform, false);
            Game.Audio = audioGo.AddComponent<AudioManager>();
            Game.UI.SetLoading(true, 0.08f, "Afinando los sonidos del mundo…");
            yield return Game.Audio.Init();

            Game.UI.SetLoading(true, 0.3f, "Pintando el cielo…");
            yield return null;
            var skyGo = new GameObject("Atmosfera");
            skyGo.transform.SetParent(transform, false);
            Game.Sky = skyGo.AddComponent<Atmosphere>();
            Game.Sky.Init();
            PostFX.Init(transform);

            Game.UI.SetLoading(true, 0.38f, "Modelando el relieve de los capítulos I y II…");
            var worldGo = new GameObject("Mundo");
            worldGo.transform.SetParent(transform, false);
            Game.World = worldGo.AddComponent<WorldStreamer>();
            yield return Game.World.Init();
            WaterSystem.Build(worldGo.transform);

            // Estado provisional para el menú (los habitantes ya siguen sus rutinas al fondo).
            NewState(null);
            Game.State.Clock.Set(1, 17.4f);

            Game.UI.SetLoading(true, 0.55f, "Despertando a los habitantes…");
            yield return null;
            var npcGo = new GameObject("Habitantes");
            npcGo.transform.SetParent(transform, false);
            Game.Npcs = npcGo.AddComponent<NpcManager>();
            Game.Npcs.Init();

            Game.Dialogue = gameObject.AddComponent<DialogueController>();
            Game.Quests = gameObject.AddComponent<QuestTracker>();
            Game.Resonance = gameObject.AddComponent<ResonanceSystem>();
            CreatePlayer();
            CreateMenuCamera();
            Doll = PaperDoll.Create();
            ExcludeDollLayer();
            Game.Settings.Apply();

            Game.UI.SetLoading(true, 0.62f, "Cargando la Tribu del Reverso…");
            yield return LoadWithProgress(MenuFocus, 0.62f, 0.98f);
            Game.Npcs.SnapAll();
            Game.UI.SetLoading(false);
            EnterMenu();
            yield return Game.UI.Fade(0f, 1.2f);
        }

        private void NewState(SaveData data)
        {
            // El contexto se crea después: así preparar el estado no dispara avisos ni sonidos.
            var s = new GameState();
            if (data == null) s.NewGame();
            else SaveConverter.Apply(data, s);
            Game.State = s;
            Game.Ctx = new RuntimeGameContext(s);
        }

        private void CreatePlayer()
        {
            _playerGo = new GameObject("Nilo");
            _playerGo.transform.SetParent(transform, false);
            _playerGo.AddComponent<CharacterController>();
            Game.Player = _playerGo.AddComponent<PlayerController>();
            Game.Cam = PlayerCamera.Create(Game.Player);
            ViewModel.Create(Game.Cam.Cam.transform);
            Game.Combat = _playerGo.AddComponent<PlayerCombat>();
            Game.Vitals = _playerGo.AddComponent<PlayerVitals>();
            Game.Interactor = _playerGo.AddComponent<PlayerInteractor>();
            Game.Listener = _playerGo.AddComponent<ResonanceListener>();
            Game.Miga = Miga.Create(transform);
            _playerGo.SetActive(false);
        }

        private void CreateMenuCamera()
        {
            var go = new GameObject("CamaraMenu");
            go.transform.SetParent(transform, false);
            go.tag = "MainCamera";
            _menuCam = go.AddComponent<Camera>();
            _menuCam.nearClipPlane = 0.3f;
            _menuCam.farClipPlane = 3200f;
            _menuCam.fieldOfView = 55f;
            go.AddComponent<AudioListener>();
            PostFX.SetupCamera(_menuCam);
            go.SetActive(false);
        }

        private void ExcludeDollLayer()
        {
            int mask = ~(1 << PaperDoll.Layer);
            if (_menuCam != null) _menuCam.cullingMask &= mask;
            if (Game.Cam != null) Game.Cam.Cam.cullingMask &= mask;
        }

        private IEnumerator LoadWithProgress(Vector3 at, float from, float to)
        {
            int before = Game.World.LoadedCount;
            var routine = StartCoroutine(Game.World.LoadAround(at, 260f));
            float t = 0f;
            bool done = false;
            StartCoroutine(Wait(routine, () => done = true));
            while (!done)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01((Game.World.LoadedCount - before) / 5f);
                Game.UI.SetLoading(true, Mathf.Lerp(from, to, Mathf.Max(k, 1f - Mathf.Exp(-t * 0.3f))));
                yield return null;
            }
        }

        private static IEnumerator Wait(Coroutine c, System.Action done)
        {
            yield return c;
            done();
        }

        // ------------------------------------------------------------------ Menú

        private void EnterMenu()
        {
            Phase = GamePhase.MainMenu;
            Time.timeScale = 1f;
            _playerGo.SetActive(false);
            _menuCam.gameObject.SetActive(true);
            Game.World.Focus = MenuFocus;
            Game.World.HasFocus = true;
            Game.UI.ShowMainMenu();
            Game.Audio.PlayMusic("musica_menu", true, 0.55f);
        }

        public void NewGame() => Run(StartGame(null));

        public void ContinueGame()
        {
            string slot = SaveSystem.MostRecent();
            var data = slot != null ? SaveSystem.Load(slot) : null;
            if (data == null)
            {
                Game.UI.Toast("No se ha podido cargar la partida.");
                return;
            }
            Run(StartGame(data));
        }

        private void Run(IEnumerator routine)
        {
            if (_busy) return;
            _flow = StartCoroutine(Guard(routine));
        }

        private IEnumerator Guard(IEnumerator routine)
        {
            _busy = true;
            yield return routine;
            _busy = false;
            _flow = null;
        }

        private IEnumerator StartGame(SaveData data)
        {
            Phase = GamePhase.Loading;
            Time.timeScale = 1f;
            Game.Dialogue.Close();
            yield return Game.UI.Fade(1f, 0.6f);
            Game.UI.CloseAll();
            Game.UI.SetLoading(true, 0.05f, data == null ? "Una mañana cualquiera en el Reverso…" : "Recuperando tus recuerdos…");
            Game.Audio.StopMusic();

            NewState(data);
            Game.Quests.ResetSession();
            Game.UI.SetBoss(null);

            Vector3 pos;
            float yaw;
            bool restSpawn = false;
            if (data == null) NewGameSpawn(out pos, out yaw);
            else
            {
                pos = new Vector3(data.PosX, data.PosY, data.PosZ);
                yaw = data.RotY;
                if (!WorldShape.InWorld(pos.x, pos.z) || pos.y < WorldShape.FallRecoveryY + 2f)
                {
                    restSpawn = true;
                    var r = WorldLayout.FindRestPoint(Game.State.LastSafePoint) ?? WorldLayout.FindRestPoint("r_casa_gina");
                    pos = K.OnGround(r.Pos);
                }
            }

            Game.World.UnloadAll();
            yield return null;
            Game.UI.SetLoading(true, 0.2f, "Cargando los alrededores (sectores de 250 m)…");
            yield return LoadWithProgress(pos, 0.2f, 0.95f);
            if (restSpawn) RestSpawn(Game.State.LastSafePoint, out pos, out yaw);

            _menuCam.gameObject.SetActive(false);
            _playerGo.SetActive(true);
            pos.y = Mathf.Max(pos.y, Ground.Height(pos.x, pos.z) + 0.05f);
            Game.Player.Teleport(pos, yaw);
            Game.Player.Frozen = false;
            Game.Vitals.ClearEffects();
            Game.Npcs.SnapAll();
            Game.World.Focus = pos;
            _regionKnown = false;
            _autosaveTimer = AutosaveInterval;
            Game.Sky.Apply(Game.State.Clock.Hour, 0f);
            Game.UI.SetLoading(false);
            Phase = GamePhase.Playing;
            Game.MarkDirty();

            if (data == null)
            {
                Game.UI.SetFade(1f);
                yield return Game.UI.Fade(1f, 0.01f, "Anoche, la Cápsula sellada de la hondonada volvió a zumbar.\nComo la noche en que Eira y Daren desaparecieron.");
                yield return new WaitForSecondsRealtime(3.2f);
                yield return Game.UI.Fade(0f, 2f);
                Game.UI.ChapterTitle("Capítulo I", "Tribu del Reverso");
                Game.Quests.Tutorial("basico");
            }
            else
            {
                yield return Game.UI.Fade(0f, 1.2f);
                Game.UI.Toast("Partida cargada: " + SaveSystem.Describe(SaveSystem.MostRecent()));
            }
        }

        private static void NewGameSpawn(out Vector3 pos, out float yaw)
        {
            var bed = WorldLayout.FindNode("gina_bed");
            var p = bed != null ? bed.Pos : WorldLayout.GinaHouse;
            pos = K.OnGround(p, 0.1f);
            yaw = OromMath.HeadingDeg(WorldLayout.Plaza.X - p.X, WorldLayout.Plaza.Z - p.Z);
        }

        /// <summary>Punto de reaparición junto a un lugar de descanso (sin quedar dentro del fuego).</summary>
        private static void RestSpawn(string id, out Vector3 pos, out float yaw)
        {
            var r = WorldLayout.FindRestPoint(id) ?? WorldLayout.FindRestPoint("r_casa_gina");
            yaw = 0f;
            if (r.Kind != "fire")
            {
                pos = K.OnGround(r.Pos, 0.1f + r.YOffset);
                yaw = OromMath.HeadingDeg(WorldLayout.Plaza.X - r.Pos.X, WorldLayout.Plaza.Z - r.Pos.Z);
                return;
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                float x = r.Pos.X + Mathf.Cos(a) * 3.2f, z = r.Pos.Z + Mathf.Sin(a) * 3.2f;
                if (!WorldShape.IsSafeGround(x, z)) continue;
                var p = K.OnGround(x, z, 0.1f);
                if (Physics.CheckCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.4f, 0.3f, ~0, QueryTriggerInteraction.Ignore)) continue;
                pos = p;
                yaw = OromMath.HeadingDeg(r.Pos.X - x, r.Pos.Z - z);
                return;
            }
            pos = K.OnGround(r.Pos.X + 3f, r.Pos.Z, 0.1f);
        }

        public void ReturnToMenu() => Run(ReturnToMenuRoutine());

        private IEnumerator ReturnToMenuRoutine()
        {
            Phase = GamePhase.Loading;
            Time.timeScale = 1f;
            yield return Game.UI.Fade(1f, 0.6f);
            Game.Dialogue.Close();
            Game.UI.CloseAll();
            Game.UI.SetBoss(null);
            _playerGo.SetActive(false);
            Game.World.UnloadAll();
            NewState(null);
            Game.State.Clock.Set(1, 17.4f);
            Game.UI.SetLoading(true, 0.3f, "Volviendo al menú…");
            yield return LoadWithProgress(MenuFocus, 0.3f, 0.95f);
            Game.Npcs.SnapAll();
            Game.UI.SetLoading(false);
            EnterMenu();
            yield return Game.UI.Fade(0f, 1f);
        }

        public void Quit()
        {
            Game.Settings?.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ Bucle de juego

        private void Update()
        {
            var s = Game.State;
            if (s == null) return;
            float dt = Time.deltaTime;

            if (Phase == GamePhase.MainMenu)
            {
                MenuCamera(Time.unscaledDeltaTime);
                Game.Sky.Apply(s.Clock.Hour, Time.unscaledDeltaTime);
                LightBudget.Update(_menuCam.transform.position, Game.Sky.Night);
                Game.Audio.SetAmbience(Region.TribuDelReverso, Game.Sky.Night, false);
                return;
            }

            bool paused = Game.UI != null && Game.UI.Paused;
            Time.timeScale = paused || Phase == GamePhase.Ending ? 0f : Time.unscaledTime < _hitStopUntil ? 0.05f : 1f;
            if (Phase != GamePhase.Playing) return;

            if (!paused) s.PlaytimeSeconds += Time.unscaledDeltaTime;
            bool talking = Game.Dialogue != null && Game.Dialogue.Active;
            if (!talking) s.Clock.Advance(dt / SecondsPerGameHour);

            var player = Game.Player.transform.position;
            Game.World.Focus = player;
            Game.Sky.Apply(s.Clock.Hour, dt);
            var camPos = Game.Cam.Cam.transform.position;
            LightBudget.Update(camPos, Game.Sky.Night);
            bool listening = Game.Listener != null && Game.Listener.IsListening;
            Game.Audio.SetAmbience(WorldShape.RegionAt(player.x, player.z), Game.Sky.Night, listening || (Game.Resonance != null && Game.Resonance.Playing));

            CheckCloudObservation(s, Time.unscaledDeltaTime);

            _checkTimer -= dt;
            if (_checkTimer <= 0f)
            {
                _checkTimer = 0.4f;
                WorldChecks(s, player);
            }

            _autosaveTimer -= dt;
            if (_autosaveTimer <= 0f)
            {
                _autosaveTimer = AutosaveInterval;
                if (!InDanger(player, 25f) && Game.Player.IsGrounded && !talking) Autosave("Automático");
                else _autosaveTimer = 30f;
            }
        }

        private void MenuCamera(float dt)
        {
            _menuOrbit += dt * 2.2f;
            float a = _menuOrbit * Mathf.Deg2Rad;
            var center = MenuFocus;
            var pos = center + new Vector3(Mathf.Cos(a) * 125f, 0f, Mathf.Sin(a) * 125f);
            pos.y = Mathf.Max(Ground.Height(pos.x, pos.z) + 25f, 78f);
            _menuCam.transform.position = pos;
            _menuCam.transform.rotation = Quaternion.LookRotation((center + Vector3.up * 2f) - pos);
        }

        private float _cloudLook;

        /// <summary>Misión «El nombre de la nube»: mirar la nube con el catalejo un momento.</summary>
        private void CheckCloudObservation(GameState s, float dt)
        {
            var cloud = Game.Sky.QuestCloud;
            if (cloud == null || s.Has("nube_observada") || s.Has("nube_nombrada") || !Game.Cam.Scoping) { _cloudLook = 0f; return; }
            var cam = Game.Cam.Cam.transform;
            var to = cloud.position - cam.position;
            if (to.magnitude > 450f || Vector3.Angle(cam.forward, to) > 9f) { _cloudLook = Mathf.Max(0f, _cloudLook - dt); return; }
            _cloudLook += dt;
            if (_cloudLook < 1.5f) return;
            Game.Ctx.SetFlag("nube_observada");
            Game.Audio.Play2D("descubrir", 0.6f);
            Game.Ctx.Notify("La nube parece un kolotapir dormido, con la tripa gris. Ya tienes algo que proponer a Bruma y Copo.");
        }

        private static bool InDanger(Vector3 p, float radius)
        {
            foreach (var c in CreatureBase.All)
                if (c != null && c.IsAlive && c.Hostile && Vector3.Distance(c.transform.position, p) < radius) return true;
            return false;
        }

        private void WorldChecks(GameState s, Vector3 p)
        {
            // Regiones
            var region = WorldShape.RegionAt(p.x, p.z);
            if (!_regionKnown || region != _region)
            {
                bool announce = _regionKnown && (region == Region.TribuDelReverso || region == Region.PradosQueRecuerdan || region == Region.ParadaAntigua || region == Region.CaminoDelEste);
                _region = region;
                _regionKnown = true;
                Game.Sky.SetRegion(region);
                if (announce) Game.UI.RegionEntered(region);
            }

            // Sectores visitados (atlas con niebla)
            if (WorldShape.InWorld(p.x, p.z))
            {
                WorldShape.SectorOf(p.x, p.z, out int sx, out int sz);
                s.World.VisitedSectors.Add(WorldShape.SectorIndex(sx, sz));
            }

            // Descubrimiento de lugares
            foreach (var loc in WorldLayout.Locations)
            {
                if (s.Knows(loc.Id)) continue;
                float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(loc.Pos.X, loc.Pos.Z));
                if (d > loc.Radius) continue;
                Game.Ctx.AddNotebook(loc.Id, Knowledge.Sabe);
                Game.UI.LocationDiscovered(loc);
            }

            // Hitos de la historia que dependen del lugar
            if (!s.Has("cruzo_puente") && p.x > 318f && p.z > 318f && region != Region.Abismo && Game.Player.IsGrounded && !WorldShape.IsOnBridgeCorridor(p.x, p.z))
                Game.Ctx.SetFlag("cruzo_puente");
            if (!s.Has("fin_capitulo_2") && s.AtStep("mq_prados", "este"))
            {
                float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(WorldLayout.EastGate.X, WorldLayout.EastGate.Z));
                if (d < 20f) Game.Ctx.SetFlag("fin_capitulo_2");
            }

            // Tutoriales contextuales
            if (WorldShape.IsOnBridgeCorridor(p.x, p.z)) Game.Quests.Tutorial("puente");
            if (Game.Player.IsBalancing) Game.Quests.Tutorial("equilibrio");
            if (Game.Sky.Night > 0.6f && s.Clock.Day >= 1 && s.PlaytimeSeconds > 120f) Game.Quests.Tutorial("noche");
            if (Game.Listener != null && Game.Listener.NearestSignal(out _) > 0.35f) Game.Quests.Tutorial("escucha");
            if (region == Region.PradosQueRecuerdan && s.HasItem("catalejo")) Game.Quests.Tutorial("catalejo");
            if (s.QuestActive("mq_silencio") && s.AtStep("mq_silencio", "entrenar")) Game.Quests.Tutorial("combate");
        }

        // ------------------------------------------------------------------ Eventos de juego

        public void HitStop(float seconds)
        {
            if (Game.Settings != null && Game.Settings.Data.ReduceMotion) seconds *= 0.5f;
            _hitStopUntil = Mathf.Max(_hitStopUntil, Time.unscaledTime + seconds);
        }

        /// <summary>Caída al mar de nubes o al abismo: Nilo vuelve al último suelo seguro (no es una derrota).</summary>
        public void RecoverFromFall()
        {
            if (_recovering || Phase != GamePhase.Playing) return;
            StartCoroutine(RecoverRoutine());
        }

        private IEnumerator RecoverRoutine()
        {
            _recovering = true;
            Game.Player.Frozen = true;
            yield return Game.UI.Fade(1f, 0.35f);
            var safe = Game.Player.LastSafePosition;
            yield return Game.World.LoadAround(safe, 200f);
            if (!WorldShape.IsSafeGround(safe.x, safe.z)) RestSpawn(Game.State.LastSafePoint, out safe, out _);
            Game.Player.Teleport(safe + Vector3.up * 0.1f, Game.Player.transform.eulerAngles.y);
            Game.Vitals.FallDamage(Game.State.Health.Max * 0.1f);
            Game.UI.Toast("Nilo se agarra a tiempo y vuelve a terreno firme.");
            yield return Game.UI.Fade(0f, 0.6f);
            Game.Player.Frozen = false;
            _recovering = false;
        }

        /// <summary>Derrota (doc 8: sin pérdida de objetos): Nilo despierta en el último lugar de descanso una hora después.</summary>
        public void PlayerDefeated()
        {
            if (_recovering || Phase != GamePhase.Playing) return;
            StartCoroutine(DefeatRoutine());
        }

        private IEnumerator DefeatRoutine()
        {
            _recovering = true;
            var s = Game.State;
            Game.Player.Frozen = true;
            Game.Dialogue.Close();
            Game.UI.CloseAll();
            Game.Cam.Shake(0.4f);
            s.Flags.Increment("derrotas");
            yield return Game.UI.Fade(1f, 1.4f, "Todo se vuelve negro…");
            Game.UI.SetBoss(null);
            var rp = WorldLayout.FindRestPoint(s.LastSafePoint) ?? WorldLayout.FindRestPoint("r_casa_gina");
            yield return Game.World.LoadAround(K.OnGround(rp.Pos), 220f);
            RestSpawn(s.LastSafePoint, out var pos, out var yaw);
            Game.Player.Teleport(pos, yaw);
            s.Clock.Advance(1f);
            s.Health.Fill();
            s.Stamina.Fill();
            Game.Vitals.ClearEffects();
            Game.Npcs.SnapAll();
            var r = WorldLayout.FindRestPoint(s.LastSafePoint);
            yield return Game.UI.Fade(1f, 0.01f, "Despiertas en " + (r != null ? r.Name.ToLowerInvariant() : "un lugar seguro") + ".\nHa pasado una hora. No has perdido nada.");
            yield return new WaitForSecondsRealtime(2.2f);
            yield return Game.UI.Fade(0f, 1.2f);
            Game.Player.Frozen = false;
            _recovering = false;
        }

        public void OnCapsuleDecision(CapsuleDecision d)
        {
            Game.Cam?.Shake(0.2f);
            Game.Sky?.Pulse(Pal.Resonance, 2.5f);
            Autosave("Decisión: " + d.Label);
        }

        // ------------------------------------------------------------------ Descanso y guardado

        public void Rest(float hours, bool sleep)
        {
            if (Phase != GamePhase.Playing || _recovering) return;
            if (InDanger(Game.Player.transform.position, 30f))
            {
                Game.UI.Toast("No puedes descansar con enemigos cerca.");
                return;
            }
            StartCoroutine(RestRoutine(hours, sleep));
        }

        private IEnumerator RestRoutine(float hours, bool sleep)
        {
            _recovering = true;
            var s = Game.State;
            Game.Player.Frozen = true;
            Game.UI.CloseAll();
            yield return Game.UI.Fade(1f, 0.8f, sleep ? "Nilo duerme…" : "El tiempo pasa…");
            s.Clock.Advance(hours);
            if (sleep || hours >= 4f) s.Health.Fill();
            else s.Health.Restore(s.Health.Max * 0.25f * hours);
            s.Stamina.Fill();
            Game.Vitals.ClearEffects();
            Game.Npcs.SnapAll();
            Game.Sky.Apply(s.Clock.Hour, 0f);
            yield return new WaitForSecondsRealtime(0.8f);
            Autosave("Descanso");
            yield return Game.UI.Fade(0f, 1f);
            Game.UI.Toast("Día " + s.Clock.Day + ", " + U.Hours(s.Clock.Hour) + ".");
            Game.Player.Frozen = false;
            _recovering = false;
        }

        private SaveData Capture(string label)
        {
            var s = Game.State;
            var p = Game.Player.IsGrounded && !Game.Player.IsSwimming ? Game.Player.transform.position : Game.Player.LastSafePosition;
            string region = WorldShape.RegionName(WorldShape.RegionAt(p.x, p.z));
            return SaveConverter.Capture(s, p.x, p.y + 0.05f, p.z, Game.Player.transform.eulerAngles.y, region, label);
        }

        public void Autosave(string label)
        {
            if (Phase != GamePhase.Playing || Game.State == null || _recovering && label != "Descanso") return;
            if (SaveSystem.Save(SaveSystem.AutoSlot, Capture(label))) Game.UI?.Toast("Guardado automático");
            _autosaveTimer = AutosaveInterval;
        }

        public bool ManualSave(RestPointDef point)
        {
            if (Phase != GamePhase.Playing || Game.State == null) return false;
            if (point != null) Game.State.LastSafePoint = point.Id;
            return SaveSystem.Save(SaveSystem.ManualSlot, Capture(point != null ? point.Name : "Manual"));
        }

        // ------------------------------------------------------------------ Final del capítulo II

        public void EndChapter()
        {
            if (Phase != GamePhase.Playing) return;
            StartCoroutine(EndRoutine());
        }

        private IEnumerator EndRoutine()
        {
            Game.Ctx.SetFlag("capitulo_2_completado");
            Autosave("Fin del capítulo II");
            yield return new WaitForSecondsRealtime(2f);
            Game.Player.Frozen = true;
            Game.Audio.PlayMusic("musica_final", false, 0.8f);
            yield return Game.UI.Fade(1f, 2.5f, "El camino del este se pierde en la bruma…");
            yield return new WaitForSecondsRealtime(1.5f);
            Phase = GamePhase.Ending;
            Game.UI.ShowEnding();
            yield return Game.UI.Fade(0f, 1.2f);
        }

        public void ContinueAfterEnding()
        {
            Game.UI.CloseAll();
            Phase = GamePhase.Playing;
            Time.timeScale = 1f;
            Game.Player.Frozen = false;
            Game.UI.Toast("Puedes seguir explorando los Prados. La historia continuará en el Capítulo III.");
        }
    }
}
