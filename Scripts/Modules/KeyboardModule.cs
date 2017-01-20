using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEngine.Experimental.EditorVR.Core;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	internal class KeyboardModule : MonoBehaviour, ICustomRay, IForEachRayOrigin
	{
		[SerializeField]
		KeyboardMallet m_KeyboardMalletPrefab;

		[SerializeField]
		KeyboardUI m_NumericKeyboardPrefab;

		[SerializeField]
		KeyboardUI m_StandardKeyboardPrefab;

		readonly Dictionary<Transform, KeyboardMallet> m_KeyboardMallets = new Dictionary<Transform, KeyboardMallet>();
		KeyboardUI m_NumericKeyboard;
		KeyboardUI m_StandardKeyboard;

		public Func<Transform, object, bool> lockRay { private get; set; }
		public Func<Transform, object, bool> unlockRay { private get; set; }
		public DefaultRayVisibilityDelegate showDefaultRay { private get; set; }
		public DefaultRayVisibilityDelegate hideDefaultRay { private get; set; }

		public Action<ForEachRayOriginCallback> forEachRayOrigin { private get; set; }

		public KeyboardUI SpawnNumericKeyboard()
		{
			if (m_StandardKeyboard != null)
				m_StandardKeyboard.gameObject.SetActive(false);

			// Check if the prefab has already been instantiated
			if (m_NumericKeyboard == null)
				m_NumericKeyboard = U.Object.Instantiate(m_NumericKeyboardPrefab.gameObject, U.Camera.GetViewerPivot()).GetComponent<KeyboardUI>();

			return m_NumericKeyboard;
		}

		public KeyboardUI SpawnAlphaNumericKeyboard()
		{
			if (m_NumericKeyboard != null)
				m_NumericKeyboard.gameObject.SetActive(false);

			// Check if the prefab has already been instantiated
			if (m_StandardKeyboard == null)
				m_StandardKeyboard = U.Object.Instantiate(m_StandardKeyboardPrefab.gameObject, U.Camera.GetViewerPivot()).GetComponent<KeyboardUI>();

			return m_StandardKeyboard;
		}

		public void SpawnKeyboardMallet(Transform rayOrigin)
		{
			var malletTransform = U.Object.Instantiate(m_KeyboardMalletPrefab.gameObject, rayOrigin).transform;
			malletTransform.position = rayOrigin.position;
			malletTransform.rotation = rayOrigin.rotation;
			var mallet = malletTransform.GetComponent<KeyboardMallet>();
			mallet.gameObject.SetActive(false);
			m_KeyboardMallets.Add(rayOrigin, mallet);
		}

		public void UpdateKeyboardMallets()
		{
			forEachRayOrigin(rayOrigin =>
			{
				var malletVisible = true;
				var numericKeyboardNull = false;
				var standardKeyboardNull = false;

				if (m_NumericKeyboard != null)
					malletVisible = m_NumericKeyboard.ShouldShowMallet(rayOrigin);
				else
					numericKeyboardNull = true;

				if (m_StandardKeyboard != null)
					malletVisible = malletVisible || m_StandardKeyboard.ShouldShowMallet(rayOrigin);
				else
					standardKeyboardNull = true;

				if (numericKeyboardNull && standardKeyboardNull)
					malletVisible = false;

				var mallet = m_KeyboardMallets[rayOrigin];

				if (mallet.visible != malletVisible)
				{
					mallet.visible = malletVisible;
					if (malletVisible)
						hideDefaultRay(rayOrigin);
					else
						showDefaultRay(rayOrigin);
				}

				// TODO remove this after physics is in
				mallet.CheckForKeyCollision();
			});
		}
	}
}