using System.Collections;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide control of scene object highlighting
    /// </summary>
    public interface IProvidesSetHighlight : IFunctionalityProvider
    {
      /// <summary>
      /// Method for highlighting objects
      /// </summary>
      /// <param name="go">The object to highlight</param>
      /// <param name="active">Whether to add or remove the highlight</param>
      /// <param name="rayOrigin">RayOrigin that hovered over the object (optional)</param>
      /// <param name="material">Custom material to use for this object</param>
      /// <param name="force">Force the setting or unsetting of the highlight</param>
      /// <param name="duration">The duration for which to show this highlight. Keep default value of 0 to show until explicitly hidden</param>
      void SetHighlight(GameObject go, bool active, Transform rayOrigin = null, Material material = null, bool force = false, float duration = 0f);

      /// <summary>
      /// Method for highlighting objects
      /// </summary>
      /// <param name="go">The object to highlight</param>
      /// <param name="active">Whether to add or remove the highlight</param>
      /// <param name="rayOrigin">RayOrigin that hovered over the object (optional)</param>
      /// <param name="material">Custom material to use for this object</param>
      /// <param name="force">Force the setting or unsetting of the highlight</param>
      /// <param name="dutyPercent">The percentage of time when the highlight is active</param>
      /// <param name="cycleDuration">The duration for which to show this highlight. Keep default value of 0 to show until explicitly hidden</param>
      /// <returns>Coroutine enumerator</returns>
      IEnumerator SetBlinkingHighlight(GameObject go, bool active, Transform rayOrigin = null,
          Material material = null, bool force = false, float dutyPercent = 0.75f, float cycleDuration = .8f);
    }
}
