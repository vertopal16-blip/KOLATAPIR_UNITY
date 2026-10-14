using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Grafo de rutas de los habitantes (A*).</summary>
    public sealed class NavGraph
    {
        private readonly Dictionary<string, Vector3> _pos = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, List<string>> _adj = new Dictionary<string, List<string>>();

        public NavGraph()
        {
            foreach (var n in WorldLayout.Nodes) { _pos[n.Id] = new Vector3(n.Pos.X, 0f, n.Pos.Z); _adj[n.Id] = new List<string>(); }
            foreach (var e in WorldLayout.Edges)
            {
                if (!_adj.ContainsKey(e.Key) || !_adj.ContainsKey(e.Value)) continue;
                _adj[e.Key].Add(e.Value);
                _adj[e.Value].Add(e.Key);
            }
        }

        public bool Has(string id) => id != null && _pos.ContainsKey(id);
        public Vector3 Pos(string id) => _pos.TryGetValue(id, out var p) ? p : Vector3.zero;

        public string Nearest(Vector3 p)
        {
            string best = null;
            float bd = float.MaxValue;
            foreach (var kv in _pos)
            {
                float d = (kv.Value - new Vector3(p.x, 0f, p.z)).sqrMagnitude;
                if (d < bd) { bd = d; best = kv.Key; }
            }
            return best;
        }

        public List<string> Path(string from, string to)
        {
            var result = new List<string>();
            if (!Has(from) || !Has(to)) return result;
            if (from == to) { result.Add(to); return result; }
            var open = new List<string> { from };
            var came = new Dictionary<string, string>();
            var g = new Dictionary<string, float> { [from] = 0f };
            var closed = new HashSet<string>();
            while (open.Count > 0)
            {
                string cur = open[0];
                float best = float.MaxValue;
                foreach (var o in open)
                {
                    float f = g[o] + Vector3.Distance(Pos(o), Pos(to));
                    if (f < best) { best = f; cur = o; }
                }
                if (cur == to) break;
                open.Remove(cur);
                closed.Add(cur);
                foreach (var nb in _adj[cur])
                {
                    if (closed.Contains(nb)) continue;
                    float ng = g[cur] + Vector3.Distance(Pos(cur), Pos(nb));
                    if (!g.TryGetValue(nb, out var old) || ng < old)
                    {
                        g[nb] = ng;
                        came[nb] = cur;
                        if (!open.Contains(nb)) open.Add(nb);
                    }
                }
            }
            if (!came.ContainsKey(to)) return result;
            var node = to;
            while (node != from) { result.Add(node); node = came[node]; }
            result.Reverse();
            return result;
        }
    }

    /// <summary>
    /// Habitante con rutina diaria, roles visibles, reacciones y diálogo contextual (doc 9 de la actualización).
    /// La simulación depende de la distancia: completa cerca, simplificada a media distancia y abstracta lejos.
    /// </summary>
    public sealed class NpcAgent : Interactable
    {
        public NpcDef Def;
        public HumanoidRig Rig;
        public int Lod;
        public bool InDialogue;
        public Emotion Emotion = Emotion.Neutral;
        public bool Talking;
        public Activity CurrentActivity { get; private set; }

        private NavGraph _graph;
        private readonly List<string> _path = new List<string>();
        private int _pathIndex;
        private string _node;
        private string _dest;
        private ScheduleEntry _entry;
        private int _routeIndex;
        private float _wait;
        private float _speed;
        private float _barkCooldown;
        private Vector3? _faceOverride;
        private float _fleeUntil;
        private float _animTime;
        private bool _visible = true;

        public override string Prompt => "Hablar con " + Def.Name;
        public override float Range => 3.2f;
        public override Vector3 FocusPoint => Rig.Head != null ? Rig.Head.position + Vector3.up * 0.1f : transform.position + Vector3.up * 1.6f;
        public Transform HeadTransform => Rig.Head;
        public bool Sleeping => CurrentActivity == Activity.Dormir && _path.Count == 0 && !InDialogue;

        public void Init(NpcDef def, NavGraph graph)
        {
            Def = def;
            _graph = graph;
            Rig = HumanoidBuilder.Build(transform, def.Species, def.Look);
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.height = def.Look.Height;
            col.radius = def.Species == Species.Kolotapir ? 0.42f : 0.28f;
            col.center = new Vector3(0f, def.Look.Height * 0.5f, 0f);
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            _barkCooldown = Random.Range(5f, 30f);
            SnapToSchedule();
        }

        private ScheduleEntry Desired()
        {
            var s = Game.State;
            var ov = Def.Override != null && s != null ? Def.Override(s) : null;
            return ov ?? Def.EntryAt(s != null ? s.Clock.Hour : 12f);
        }

        /// <summary>Coloca al personaje directamente donde debería estar (modo abstracto o al cargar).</summary>
        public void SnapToSchedule()
        {
            _entry = Desired();
            _dest = _entry.Node;
            _node = _dest;
            _path.Clear();
            var p = _graph.Pos(_dest);
            transform.position = new Vector3(p.x, Ground.Height(p.x, p.z), p.z);
            CurrentActivity = _entry.Activity;
            FaceForActivity(1f);
            ApplyPoseNow();
        }

        public void SetVisible(bool v)
        {
            if (_visible == v) return;
            _visible = v;
            Rig.Root.gameObject.SetActive(v);
        }

        public override bool CanInteract => _visible && !InDialogue;

        public override void Interact()
        {
            if (Game.Dialogue == null) return;
            foreach (var c in CreatureBase.All)
                if (c != null && c.IsAlive && c.Hostile && Vector3.Distance(c.transform.position, transform.position) < 14f)
                {
                    Game.UI?.Subtitle(Def.Name, Def.Species == Species.Kolotapir ? "¡Hmmf! ¡No es momento de charlar!" : "¡Ahora no! ¡Cuidado!", 2.5f);
                    return;
                }
            Game.Dialogue.StartWith(this);
        }

        public void Tick(float dt, Vector3 playerPos)
        {
            _animTime += dt;
            _barkCooldown -= dt;
            var desired = Desired();
            if (desired != _entry && !InDialogue)
            {
                _entry = desired;
                _routeIndex = 0;
                GoTo(desired.Node);
            }

            if (Lod >= 2) return;
            float playerDist = Vector3.Distance(transform.position, playerPos);

            if (InDialogue)
            {
                _speed = 0f;
                _faceOverride = playerPos;
                Face(playerPos, dt, 6f);
            }
            else if (Time.time < _fleeUntil)
            {
                MoveAlongPath(dt, Def.WalkSpeed * 2.2f);
            }
            else if (_path.Count > 0)
            {
                // Cede el paso si Nilo está justo delante
                var ahead = transform.position + transform.forward * 0.8f;
                if (Vector3.Distance(ahead, playerPos) < 0.75f && playerDist < 1.4f) { _speed = 0f; Face(playerPos, dt, 4f); }
                else MoveAlongPath(dt, (CurrentActivity == Activity.Jugar ? 1.6f : 1f) * Def.WalkSpeed);
            }
            else
            {
                _speed = 0f;
                ArrivedBehaviour(dt);
            }

            if (Lod == 0)
            {
                bool look = playerDist < 4.5f && !Sleeping && CurrentActivity != Activity.Dormir;
                Vector3? lookAt = InDialogue || look ? playerPos + Vector3.up * 1.5f : (Vector3?)null;
                bool talking = Talking || (InDialogue && Game.Dialogue != null && Game.Dialogue.SpeakerIs(this));
                if (Sleeping) PoseSleeping(dt);
                else HumanoidAnimator.Pose(Rig, _animTime, _speed, CurrentActivity, Emotion, talking, lookAt, dt);
                if (playerDist < 5.5f && _barkCooldown <= 0f && !InDialogue && !Sleeping) Bark();
            }
            else if (!Sleeping) HumanoidAnimator.Pose(Rig, _animTime, _speed, CurrentActivity, Emotion, false, null, dt);
        }

        private void GoTo(string node)
        {
            _dest = node;
            _path.Clear();
            var from = _node ?? _graph.Nearest(transform.position);
            var p = _graph.Path(from, node);
            if (p.Count == 0 && from != node) p.Add(node);
            _path.AddRange(p);
            _pathIndex = 0;
            CurrentActivity = _entry.Activity == Activity.Jugar || _entry.Activity == Activity.Patrullar || _entry.Activity == Activity.Pasear ? _entry.Activity : Activity.Pasear;
            ResetRig();
        }

        private void MoveAlongPath(float dt, float speed)
        {
            if (_pathIndex >= _path.Count) { _path.Clear(); OnArrive(); return; }
            var target = _graph.Pos(_path[_pathIndex]);
            var p = transform.position;
            var to = new Vector3(target.x - p.x, 0f, target.z - p.z);
            float d = to.magnitude;
            if (d < 0.25f)
            {
                _node = _path[_pathIndex];
                _pathIndex++;
                return;
            }
            var step = to / d * Mathf.Min(d, speed * dt);
            var next = p + step;
            next.y = Ground.Height(next.x, next.z);
            transform.position = next;
            Face(transform.position + to, dt, 8f);
            _speed = speed;
        }

        private void OnArrive()
        {
            _node = _dest;
            CurrentActivity = _entry.Activity;
            _wait = Random.Range(4f, 9f);
            if (_entry.Activity == Activity.Dormir) ResetRig();
            FaceForActivity(0f);
        }

        private void ArrivedBehaviour(float dt)
        {
            if (_faceOverride.HasValue) { Face(_faceOverride.Value, dt, 4f); _faceOverride = null; }
            else if (_faceTarget.HasValue) Face(_faceTarget.Value, dt, 3f);
            if (_entry.Route != null && _entry.Route.Length > 0 && CurrentActivity != Activity.Dormir)
            {
                _wait -= dt;
                if (_wait <= 0f)
                {
                    _routeIndex = (_routeIndex + 1) % _entry.Route.Length;
                    var next = _entry.Route[_routeIndex];
                    if (_graph.Has(next) && next != _node)
                    {
                        _dest = next;
                        _path.Clear();
                        _path.AddRange(_graph.Path(_node, next));
                        _pathIndex = 0;
                    }
                    else _wait = 3f;
                }
            }
        }

        private Vector3? _faceTarget;

        private void FaceForActivity(float instant)
        {
            _faceTarget = null;
            if (_node == null) return;
            var plaza = K.V3(WorldLayout.Plaza);
            if (_node.StartsWith("plaza")) _faceTarget = plaza;
            else if (_node == "workshop") _faceTarget = K.V3(WorldLayout.Workshop);
            else if (_node == "boro_fire" || _node == "bc_fire") _faceTarget = _node == "boro_fire" ? K.V3(WorldLayout.BoroCamp) : K.V3(WorldLayout.KolotapirCamp);
            else if (_node.StartsWith("hill_top")) _faceTarget = new Vector3(790f, 0f, 470f);
            else if (_node == "stones") _faceTarget = K.V3(WorldLayout.MigrationStones);
            else if (_node == "tree_seat") _faceTarget = new Vector3(205f, 0f, 205f);
            if (_faceTarget.HasValue && instant > 0f)
            {
                var to = _faceTarget.Value - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
            }
        }

        private void Face(Vector3 target, float dt, float rate)
        {
            var to = target - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.001f) transform.rotation = K.Damp(transform.rotation, Quaternion.LookRotation(to), rate, dt);
        }

        private void ResetRig()
        {
            Rig.Root.localPosition = Vector3.zero;
            Rig.Root.localRotation = Quaternion.identity;
        }

        private void PoseSleeping(float dt)
        {
            // Tumbado en la cama: la cama está al fondo de la casa, orientada de lado.
            if (_node == null || !_node.EndsWith("_bed")) { HumanoidAnimator.Pose(Rig, _animTime, 0f, Activity.Sentarse, Emotion.Neutral, false, null, dt); return; }
            var bed = _graph.Pos(_node);
            var houseId = _node.Substring(0, _node.Length - 4);
            var houseIn = _graph.Pos(houseId + "_in");
            var away = (bed - houseIn); away.y = 0f;
            var right = Vector3.Cross(Vector3.up, away.normalized);
            var feet = bed - right * (Def.Look.Height * 0.5f);
            feet.y = Ground.Height(bed.x, bed.z) + 0.6f;
            Rig.Root.position = feet;
            Rig.Root.rotation = Quaternion.LookRotation(Vector3.up, right);
            HumanoidAnimator.Pose(Rig, _animTime * 0.3f, 0f, Activity.Casa, Emotion.Neutral, false, null, dt);
        }

        private void ApplyPoseNow()
        {
            if (CurrentActivity == Activity.Dormir) PoseSleeping(1f);
            else { ResetRig(); HumanoidAnimator.Pose(Rig, _animTime, 0f, CurrentActivity, Emotion, false, null, 1f); }
        }

        private void Bark()
        {
            _barkCooldown = Random.Range(60f, 120f);
            if (!NpcManager.CanBark()) return;
            var s = Game.State;
            var options = new List<AmbientLine>();
            foreach (var b in Def.Barks) if (b.Condition == null || b.Condition(s)) options.Add(b);
            if (options.Count == 0) return;
            var line = options[Random.Range(0, options.Count)];
            Game.UI?.Subtitle(Def.Name, line.Text, 3f);
            Game.Audio?.Voice(Def.Species, Def.VoicePitch, Mathf.Clamp(line.Text.Length / 10, 2, 6));
            Talking = true;
            Invoke(nameof(StopTalking), 2.5f);
        }

        private void StopTalking() => Talking = false;

        public void SayLine(string text, float duration)
        {
            Game.UI?.Subtitle(Def.Name, text, duration);
            Game.Audio?.Voice(Def.Species, Def.VoicePitch, Mathf.Clamp(text.Length / 10, 2, 6));
            Talking = true;
            CancelInvoke(nameof(StopTalking));
            Invoke(nameof(StopTalking), Mathf.Min(duration, 3f));
        }

        /// <summary>Reacción ante el peligro: corre a refugiarse.</summary>
        public void Flee(string bark)
        {
            if (Time.time < _fleeUntil || InDialogue) return;
            _fleeUntil = Time.time + 8f;
            var safe = Def.HomeNode;
            if (_graph.Has(safe) && _node != null)
            {
                _path.Clear();
                _path.AddRange(_graph.Path(_node, safe));
                _pathIndex = 0;
                _dest = safe;
                _entry = Desired();
            }
            if (!string.IsNullOrEmpty(bark) && Lod == 0) SayLine(bark, 2.5f);
        }

        public void ReactToSwing()
        {
            if (Lod != 0 || InDialogue) return;
            if (_barkCooldown > 50f) return;
            _barkCooldown = 70f;
            SayLine(Def.Species == Species.Kolotapir ? "¡Hmmf! ¡Cuidado con esa espada!" : "¡Eh! ¡Cuidado con eso!", 2.2f);
            Emotion = Emotion.Sorprendido;
            Invoke(nameof(CalmDown), 3f);
        }

        private void CalmDown() => Emotion = Emotion.Neutral;
    }

    /// <summary>
    /// Gestiona a los habitantes: simulación por distancia, conversaciones ambientales y reacciones
    /// (doc: "cerca del jugador: simulación completa; a media distancia: rutinas simplificadas; lejos: actualización abstracta").
    /// </summary>
    public sealed class NpcManager : MonoBehaviour
    {
        public readonly List<NpcAgent> Agents = new List<NpcAgent>();
        private NavGraph _graph;
        private float _convTimer = 5f;
        private float _dangerTimer;
        private int _frame;
        private float _abstractTimer;
        private static float _lastBark;
        public int[] LodCounts = new int[3];
        private readonly Dictionary<string, float> _convPlayed = new Dictionary<string, float>();
        private bool _conversing;

        public NavGraph Graph => _graph;

        public static bool CanBark()
        {
            if (Time.time - _lastBark < 10f) return false;
            if (Game.Dialogue != null && Game.Dialogue.Active) return false;
            _lastBark = Time.time;
            return true;
        }

        public void Init()
        {
            _graph = new NavGraph();
            foreach (var def in GameDb.Npcs.Values)
            {
                var go = new GameObject("NPC_" + def.Id);
                go.transform.SetParent(transform, false);
                var agent = go.AddComponent<NpcAgent>();
                agent.Init(def, _graph);
                Agents.Add(agent);
            }
        }

        public NpcAgent Find(string id)
        {
            foreach (var a in Agents) if (a.Def.Id == id) return a;
            return null;
        }

        public void SnapAll()
        {
            foreach (var a in Agents) a.SnapToSchedule();
        }

        private void Update()
        {
            if (Game.State == null || Agents.Count == 0) return;
            var focus = Game.Player != null && Game.InGame ? Game.Player.transform.position : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            float dt = Time.deltaTime;
            _frame++;
            _abstractTimer -= dt;
            LodCounts[0] = LodCounts[1] = LodCounts[2] = 0;
            foreach (var a in Agents)
            {
                float d = K.FlatDistance(a.transform.position, focus);
                bool loaded = Game.World == null || Game.World.IsLoadedAt(a.transform.position.x, a.transform.position.z);
                int lod = !loaded || d > 170f ? 2 : d > 45f ? 1 : 0;
                if (a.InDialogue) lod = 0;
                if (a.Lod == 2 && lod < 2) a.SnapToSchedule();
                a.Lod = lod;
                a.SetVisible(lod < 2);
                LodCounts[lod]++;
                if (lod == 0) a.Tick(dt, focus);
                else if (lod == 1) { if (_frame % 4 == (a.GetInstanceID() & 3)) a.Tick(dt * 4f, focus); }
                else if (_abstractTimer <= 0f) a.Tick(1f, focus);
            }
            if (_abstractTimer <= 0f) _abstractTimer = 1f;

            _convTimer -= dt;
            if (_convTimer <= 0f) { _convTimer = 4f; TryConversation(focus); }
            _dangerTimer -= dt;
            if (_dangerTimer <= 0f) { _dangerTimer = 1f; CheckDanger(); }
        }

        private void TryConversation(Vector3 focus)
        {
            if (_conversing || (Game.Dialogue != null && Game.Dialogue.Active) || !Game.InGame) return;
            var s = Game.State;
            foreach (var c in GameDb.Conversations)
            {
                if (c.Condition != null && !c.Condition(s)) continue;
                if (_convPlayed.TryGetValue(c.Id, out var t) && Time.time - t < 600f) continue;
                var a = Find(c.NpcA);
                var b = Find(c.NpcB);
                if (a == null || b == null || a.Lod != 0 || b.Lod != 0 || a.Sleeping || b.Sleeping || a.InDialogue || b.InDialogue) continue;
                if (Vector3.Distance(a.transform.position, b.transform.position) > 7f) continue;
                if (Vector3.Distance(a.transform.position, focus) > 14f) continue;
                _convPlayed[c.Id] = Time.time;
                StartCoroutine(PlayConversation(c, a, b));
                return;
            }
        }

        private System.Collections.IEnumerator PlayConversation(AmbientConversation c, NpcAgent a, NpcAgent b)
        {
            _conversing = true;
            foreach (var line in c.Lines)
            {
                var speaker = line.Speaker == a.Def.Name ? a : b;
                speaker.SayLine(line.Text, 3.4f);
                yield return new WaitForSeconds(3.6f);
                if (Game.Dialogue != null && Game.Dialogue.Active) break;
            }
            _conversing = false;
        }

        public void OnPlayerSwing(Vector3 pos)
        {
            foreach (var a in Agents)
                if (a.Lod == 0 && Vector3.Distance(a.transform.position, pos) < 3f) a.ReactToSwing();
        }

        private void CheckDanger()
        {
            foreach (var a in Agents)
            {
                if (a.Lod == 2) continue;
                foreach (var c in CreatureBase.All)
                {
                    if (c == null || !c.IsAlive || !(c is EchoEnemy)) continue;
                    if (Vector3.Distance(c.transform.position, a.transform.position) < 12f)
                    {
                        a.Flee(a.Def.Species == Species.Kolotapir ? "¡Ecos! ¡Al fuego!" : "¡Ecos!");
                        break;
                    }
                }
            }
        }
    }
}
