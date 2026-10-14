using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    // ------------------------------------------------------------------ Misiones

    public sealed class QuestStep
    {
        public string Id;
        public string Text;
        public Func<GameState, bool> Done;
        public int Xp;
        public Action<IGameContext> OnComplete;
        /// <summary>Pistas graduales (de vaga a explícita). Doc: "Pistas graduales para puzles".</summary>
        public string[] Hints = new string[0];
        /// <summary>Lugar u objetivo hacia el que Miga revolotea al pedir una pista.</summary>
        public string HintTarget;
        /// <summary>Progreso detallado opcional (p. ej. "Pistas 2/3 · Recuerdos 1/2").</summary>
        public Func<GameState, string> Detail;
    }

    public sealed class QuestDef
    {
        public string Id;
        public string Title;
        public string Chapter = "";
        public string Summary;
        public QuestKind Kind;
        public List<QuestStep> Steps = new List<QuestStep>();
        public int CompletionXp;
        public Action<IGameContext> OnComplete;
    }

    // ------------------------------------------------------------------ Diálogo

    public sealed class DialogueChoice
    {
        public string Text;
        public string Next;
        public Func<GameState, bool> Condition;
        public Action<IGameContext> Effect;
        /// <summary>Texto que explica por qué no está disponible (si no se oculta).</summary>
        public string LockedReason;
        public bool HideWhenLocked = true;
    }

    public sealed class DialogueNode
    {
        public string Id;
        public string Speaker;
        public string Text;
        public Emotion Emotion = Emotion.Neutral;
        public string Next;
        public Action<IGameContext> OnEnter;
        public List<DialogueChoice> Choices = new List<DialogueChoice>();
    }

    public sealed class DialogueDef
    {
        public string Id;
        public string Start = "start";
        public Dictionary<string, DialogueNode> Nodes = new Dictionary<string, DialogueNode>();
    }

    // ------------------------------------------------------------------ Cuaderno

    public sealed class NotebookEntryDef
    {
        public string Id;
        public NotebookCategory Category;
        public string Title;
        public string Text;
        /// <summary>Texto adicional cuando la certeza sube a "Sabe" (si es distinto).</summary>
        public string KnownText;
        public int Xp = 8;
    }

    public sealed class EvidenceDef
    {
        public string Id;
        public string Text;
        public Knowledge Certainty = Knowledge.Observado;
    }

    public sealed class HypothesisDef
    {
        public string Id;
        public string Text;
        public string[] SupportedBy = new string[0];
        public string[] RefutedBy = new string[0];
    }

    public sealed class MysteryDef
    {
        public string Id;
        public string Question;
        public List<EvidenceDef> Evidence = new List<EvidenceDef>();
        public List<HypothesisDef> Hypotheses = new List<HypothesisDef>();
        public string TrueHypothesis;
        /// <summary>Evidencia que resuelve el misterio al obtenerse.</summary>
        public string ResolvingEvidence;
        public string Resolution;
        public int Xp = 40;
    }

    // ------------------------------------------------------------------ Resonancias y Cápsulas

    public sealed class ResonanceLine
    {
        public string Speaker;
        public string Text;
        public ResonanceLine(string speaker, string text) { Speaker = speaker; Text = text; }
    }

    public sealed class ResonanceDef
    {
        public string Id;
        public ResonanceType Type;
        public string Title;
        public List<ResonanceLine> Lines = new List<ResonanceLine>();
        /// <summary>Observación que Nilo anota tras escuchar (no una interpretación cerrada).</summary>
        public string Note;
        public Func<GameState, bool> Requirement;
        public string RequirementText;
        /// <summary>Memoria privada: escucharla sin consentimiento tiene consecuencias.</summary>
        public bool IsPrivate;
        public string OwnerNpc;
        public string ConsentFlag;
        public Action<IGameContext> OnHeard;
        public int Xp = 15;
        public string[] Evidence = new string[0];
    }

    public sealed class MemoryFragment
    {
        public string Id;
        public string Symbol;
        public string Text;
    }

    public sealed class CapsuleDecision
    {
        public string Id;
        public string Label;
        public string Description;
        public ConsequenceScale Scale;
        public CapsuleState ResultState;
        public Action<IGameContext> Effect;
    }

    public sealed class CapsuleDef
    {
        public string Id;
        public string Name;
        public CapsuleState InitialState;
        public string Description;
        public Dictionary<CapsuleState, string> ExamineText = new Dictionary<CapsuleState, string>();
        /// <summary>Resonancia que se puede escuchar directamente en la Cápsula según su estado.</summary>
        public Dictionary<CapsuleState, string> ListenResonance = new Dictionary<CapsuleState, string>();
        /// <summary>Resonancias que se escuchan en orden (la primera no oída cuyo requisito se cumpla).</summary>
        public List<string> ListenSequence = new List<string>();
        public Action<IGameContext> OnExamine;
        public List<ItemAmount> RepairCost = new List<ItemAmount>();
        public Func<GameState, bool> RepairAvailable;
        public string RepairFlag;
        public string RepairText;
        public List<MemoryFragment> Fragments = new List<MemoryFragment>();
        public string[] CorrectOrder = new string[0];
        public string PuzzleIntro;
        public string[] PuzzleHints = new string[0];
        public Func<GameState, bool> PuzzleAvailable;
        public string PuzzleLockedText;
        public string PuzzleSolvedFlag;
        public Action<IGameContext> OnPuzzleSolved;
        public List<CapsuleDecision> Decisions = new List<CapsuleDecision>();
        public Func<GameState, bool> DecisionAvailable;
        public string DecisionFlag;
        public bool PrivateMemory;
    }

    // ------------------------------------------------------------------ NPC

    public sealed class ScheduleEntry
    {
        public float Start;
        public float End;
        public Activity Activity;
        public string Node;
        /// <summary>Nodos adicionales para patrullar o pasear.</summary>
        public string[] Route;
        public ScheduleEntry(float start, float end, Activity activity, string node, params string[] route)
        {
            Start = start; End = end; Activity = activity; Node = node; Route = route ?? new string[0];
        }

        public bool Contains(float hour)
        {
            return Start <= End ? hour >= Start && hour < End : hour >= Start || hour < End;
        }
    }

    public sealed class Appearance
    {
        public float Height = 1.7f;
        public float Build = 1f;
        public string Skin = "#8D5A3B";
        public string Hair = "#1A1410";
        public string Eyes = "#2B1B10";
        public string Cloth = "#B9A27E";
        public string Cloth2 = "#7A4B2E";
        public string Accent = "#B5523B";
        public string Pants = "#4E5A4A";
        public string HairStyle = "corto";
        public bool Beard;
        public bool Staff;
        public bool Backpack;
        public bool Hat;
        public bool Apron;
        public bool Shawl;
        public bool Basket;
    }

    public sealed class AmbientLine
    {
        public string Text;
        public Func<GameState, bool> Condition;
        public AmbientLine(string text, Func<GameState, bool> condition = null) { Text = text; Condition = condition; }
    }

    public sealed class NpcDef
    {
        public string Id;
        public string Name;
        public Species Species;
        public string Role;
        public string Culture;
        public Appearance Look = new Appearance();
        public List<ScheduleEntry> Schedule = new List<ScheduleEntry>();
        public float VoicePitch = 1f;
        public float WalkSpeed = 1.35f;
        public Func<GameState, string> SelectDialogue;
        public List<AmbientLine> Barks = new List<AmbientLine>();
        public string HomeNode;
        public string NotebookEntry;
        public string MerchantId;
        public bool QuestCritical;
        /// <summary>Cambio de rutina por acontecimientos (doc: rutinas diarias y cambios por acontecimientos).</summary>
        public Func<GameState, ScheduleEntry> Override;

        public ScheduleEntry EntryAt(float hour)
        {
            for (int i = 0; i < Schedule.Count; i++)
                if (Schedule[i].Contains(hour)) return Schedule[i];
            return Schedule.Count > 0 ? Schedule[0] : null;
        }
    }

    /// <summary>Conversación ambiental entre dos habitantes cuando coinciden (doc: conversaciones que revelan la cultura).</summary>
    public sealed class AmbientConversation
    {
        public string Id;
        public string NpcA;
        public string NpcB;
        public Func<GameState, bool> Condition;
        public List<ResonanceLine> Lines = new List<ResonanceLine>();
    }
}
