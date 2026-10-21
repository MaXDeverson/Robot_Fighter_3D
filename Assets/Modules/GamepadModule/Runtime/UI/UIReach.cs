using System.Collections.Generic;
using UnityEngine;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Whether a UI element would be reached by a finger right now, as far as its <see cref="CanvasGroup"/>s say.
    /// <para>
    /// A pad presses HUD buttons without a raycast, so nothing stops it at a group that blocks raycasts the way a finger
    /// is stopped. The HUD is locked through exactly such groups — the whole interface under one during a tutorial step,
    /// a single control under its own — and a pad press has to ask here so that what is locked for the finger is
    /// locked for the pad too. Groups are read the way the raycaster reads them: up to the first one that ignores its
    /// parents.
    /// </para>
    /// </summary>
    public static class UIReach
    {
        private static readonly List<CanvasGroup> Groups = new List<CanvasGroup>();

        public static bool IsReachable(Component element)
        {
            if (element == null || !element.gameObject.activeInHierarchy)
                return false;

            Transform current = element.transform;
            while (current != null)
            {
                current.GetComponents(Groups);
                bool ignoresParents = false;

                for (int i = 0; i < Groups.Count; i++)
                {
                    CanvasGroup group = Groups[i];
                    if (!group.enabled)
                        continue;

                    if (!group.blocksRaycasts)
                    {
                        Groups.Clear();
                        return false;
                    }

                    ignoresParents |= group.ignoreParentGroups;
                }

                Groups.Clear();

                if (ignoresParents)
                    return true;

                current = current.parent;
            }

            return true;
        }
    }
}
