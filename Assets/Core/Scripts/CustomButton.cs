using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core
{
    public class CustomButton : Button
    {
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            OnCustomClick();

            base.OnPointerClick(eventData);
        }

        private void OnCustomClick()
        {
            GameLog.Log("Button clicked: " + gameObject.name);

            AudioAndHapticManager.Instance.PlayEffectAndHapticButton();

            // SoundManager.Instance.PlayButtonClick();
        }
    }
}
