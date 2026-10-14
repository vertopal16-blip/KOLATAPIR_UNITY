using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Todo lo que Nilo puede usar con la tecla de interacción. Se registra solo al activarse.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly HashSet<Interactable> All = new HashSet<Interactable>();

        public virtual string Prompt => "Interactuar";
        public virtual bool CanInteract => true;
        public virtual float Range => 2.6f;
        public virtual Vector3 FocusPoint => transform.position;
        /// <summary>Si es falso, la interacción no exige mirar directamente al objeto (p. ej. una cama grande).</summary>
        public virtual bool NeedsAim => true;

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public abstract void Interact();
    }

    /// <summary>Objeto que se recoge una vez (único, persistente) o un recurso que reaparece tras un día.</summary>
    public sealed class PickupItem : Interactable
    {
        public string Id;
        public string ItemId;
        public int Count = 1;
        public bool Renewable;
        public string VerbOverride;
        private float _checkTimer;
        private bool _hidden;
        private Transform _visual;

        public override string Prompt
        {
            get
            {
                var def = GameDb.Item(ItemId);
                string name = def != null ? def.Name : ItemId;
                if (!string.IsNullOrEmpty(VerbOverride)) return VerbOverride + " " + name.ToLowerInvariant();
                return (Renewable ? "Recolectar " : "Recoger ") + name.ToLowerInvariant() + (Count > 1 ? $" ×{Count}" : "");
            }
        }

        public override bool CanInteract => !_hidden;

        private void Start()
        {
            _visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            Refresh();
        }

        private void Update()
        {
            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f) return;
            _checkTimer = 3f;
            Refresh();
        }

        private void Refresh()
        {
            var s = Game.State;
            if (s == null) return;
            bool gone = Renewable ? s.World.IsDefeated(Id, s.Clock.TotalHours, false) : s.World.IsTaken(Id);
            SetHidden(gone);
        }

        private void SetHidden(bool hidden)
        {
            _hidden = hidden;
            foreach (Transform t in transform) t.gameObject.SetActive(!hidden);
        }

        public override void Interact()
        {
            if (_hidden) return;
            var s = Game.State;
            int count = Count;
            if (Renewable && s.HasSkill("recolector") && Random.value < 0.3f) count++;
            Game.Ctx.GiveItem(ItemId, count);
            if (Renewable) s.World.MarkDefeated(Id, s.Clock.TotalHours);
            else s.World.MarkTaken(Id);
            Game.Audio?.Play3D(Renewable ? "recolectar" : "recoger", transform.position, 0.8f);
            SetHidden(true);
            Game.MarkDirty();
        }
    }

    /// <summary>Cofre o arcón con contenido persistente.</summary>
    public sealed class ChestInteractable : Interactable
    {
        public ChestDef Def;
        public Transform Lid;
        private bool _open;

        public override string Prompt => _open ? "Vacío" : "Abrir " + Def.Name.ToLowerInvariant();
        public override bool CanInteract => !_open;

        private void Start()
        {
            _open = Game.State != null && Game.State.World.IsTaken(Def.Id);
            if (_open && Lid != null) Lid.localRotation = Quaternion.Euler(-100f, 0f, 0f);
        }

        public override void Interact()
        {
            if (_open) return;
            _open = true;
            foreach (var it in Def.Contents) Game.Ctx.GiveItem(it.ItemId, it.Count);
            Game.State.World.MarkTaken(Def.Id);
            Game.Audio?.Play3D("cofre", transform.position);
            if (Lid != null) StartCoroutine(OpenLid());
            Game.MarkDirty();
        }

        private System.Collections.IEnumerator OpenLid()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.5f;
                Lid.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -100f, Mathf.SmoothStep(0f, 1f, t)), 0f, 0f);
                yield return null;
            }
        }
    }

    /// <summary>Punto seguro: descansar, pasar el tiempo, guardar y reaparecer aquí.</summary>
    public sealed class RestPointInteractable : Interactable
    {
        public RestPointDef Def;
        public override string Prompt => Def.Kind == "bed" ? "Descansar en la cama" : "Descansar junto al fuego";
        public override float Range => 3.2f;
        public override bool NeedsAim => false;

        public override void Interact() => Game.UI.OpenRestMenu(Def);
    }

    /// <summary>Algo que se puede observar con detenimiento (pistas, inscripciones, marcas).</summary>
    public sealed class ExamineSpot : Interactable
    {
        public ExamineDef Def;
        private float _checkTimer;
        private bool _active = true;

        public override string Prompt => Def.Prompt == "Examinar" ? "Examinar: " + Def.Title.ToLowerInvariant() : Def.Prompt;
        public override bool CanInteract => _active;
        public override float Range => 2.4f;

        private void Update()
        {
            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f) return;
            _checkTimer = 0.5f;
            bool active = Game.State != null && (Def.Active == null || Def.Active(Game.State));
            if (Def.ConsumeOnce && Game.State != null && Game.State.World.IsTaken(Def.Id)) active = false;
            if (active == _active) return;
            _active = active;
            foreach (Transform t in transform) t.gameObject.SetActive(active);
        }

        public override void Interact()
        {
            if (!_active) return;
            var s = Game.State;
            Game.UI.ShowExamine(Def.Title, Tokens.Apply(Def.Text));
            if (!s.Flags.Has("examined_" + Def.Id))
            {
                s.Flags.Set("examined_" + Def.Id);
                Def.OnExamine?.Invoke(Game.Ctx);
            }
            if (Def.ConsumeOnce) s.World.MarkTaken(Def.Id);
            Game.Audio?.Play2D("examinar", 0.6f);
            Game.MarkDirty();
        }
    }

    /// <summary>Mesa de trabajo (Tomé, Boro): fabricación y mejoras.</summary>
    public sealed class CraftingStation : Interactable
    {
        public string Owner = "Tomé";
        public override string Prompt => "Usar la mesa de trabajo";

        public override void Interact()
        {
            if (!Game.State.Has("receta_basica"))
            {
                Game.UI.ShowExamine("Mesa de trabajo", $"Herramientas, cuerdas y retales de cuero. Todavía no sabes usarla bien: {Owner} podría enseñarte.");
                if (!Game.State.Quests.IsKnown("sq_tome")) Game.Ctx.Notify("Habla con Tomé, el artesano del taller.");
                return;
            }
            Game.UI.OpenCrafting(true);
        }
    }

    /// <summary>Cortina de enredaderas: una herramienta cotidiana (la navaja) abre el camino.</summary>
    public sealed class VineCurtain : Interactable
    {
        public string Id = "vines_gruta";
        public override string Prompt => Game.State != null && Game.State.HasItem("navaja") ? "Cortar las enredaderas (navaja)" : "Enredaderas espesas";

        private void Start()
        {
            if (Game.State != null && Game.State.World.IsTaken(Id)) gameObject.SetActive(false);
        }

        public override void Interact()
        {
            if (!Game.State.HasItem("navaja"))
            {
                Game.Ctx.Notify("Necesitas algo para cortarlas.");
                return;
            }
            Game.State.World.MarkTaken(Id);
            Game.Audio?.Play3D("cortar", transform.position);
            Game.Ctx.AddNotebook("loc_gruta", Knowledge.Observado);
            gameObject.SetActive(false);
        }
    }

    /// <summary>Asiento: sentarse a mirar el paisaje (y recuperar aguante más rápido).</summary>
    public sealed class Seat : Interactable
    {
        public override string Prompt => "Sentarse";
        public override float Range => 2.2f;
        public override void Interact() => Game.Player?.SitAt(this);
    }

    /// <summary>Sustituye {Accion} por la tecla asignada en el texto del contenido.</summary>
    public static class Tokens
    {
        public static string Apply(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            foreach (InputAction a in System.Enum.GetValues(typeof(InputAction)))
            {
                string token = "{" + a + "}";
                if (text.Contains(token)) text = text.Replace(token, "[" + InputMap.KeyName(a) + "]");
            }
            return text;
        }
    }
}
