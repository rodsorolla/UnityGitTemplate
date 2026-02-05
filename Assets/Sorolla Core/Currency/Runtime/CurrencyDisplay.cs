using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Sorolla.Currency
{
    /// <summary>
    /// UI component that displays a currency balance with animated value transitions.
    /// Subscribes to currency changes and updates automatically.
    /// </summary>
    public class CurrencyDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text _amountText;

        [Header("Configuration")]
        [SerializeField] private string _currencyId = CurrencyIds.Coins;
        [SerializeField] private float _animationDuration = 0.5f;

        [Header("Coin FX (Optional)")]
        [SerializeField] private ParticleSystem _addFX;

        private ICurrencyService _currencyService;
        private int _displayedValue;
        private Tween _countTween;

        private void OnEnable()
        {
            // Show 0 immediately
            _displayedValue = 0;
            UpdateText(0);

            TrySubscribe();
        }

        private void Start()
        {
            // Retry if service wasn't ready in OnEnable
            if (_currencyService == null)
            {
                TrySubscribe();
            }
        }

        private void TrySubscribe()
        {
            _currencyService = ServiceLocator.Instance.TryResolve<ICurrencyService>();

            if (_currencyService != null)
            {
                _currencyService.OnCurrencyChanged += HandleCurrencyChanged;

                // Update to actual value (no animation)
                _displayedValue = _currencyService.GetBalance(_currencyId);
                UpdateText(_displayedValue);
            }
        }

        private void OnDisable()
        {
            _countTween?.Kill();

            if (_currencyService != null)
            {
                _currencyService.OnCurrencyChanged -= HandleCurrencyChanged;
            }
        }

        private void HandleCurrencyChanged(CurrencyChangedEventArgs args)
        {
            if (args.CurrencyId != _currencyId) return;

            // Play FX only when adding currency
            if (args.Delta > 0 && _addFX != null)
            {
                _addFX.Stop();
                _addFX.Play();
            }

            AnimateToValue(args.NewBalance);
        }

        private void AnimateToValue(int targetValue)
        {
            _countTween?.Kill();

            _countTween = DOTween.To(
                () => _displayedValue,
                x =>
                {
                    _displayedValue = x;
                    UpdateText(x);
                },
                targetValue,
                _animationDuration
            ).SetEase(Ease.OutQuad)
            .SetDelay(1);
        }

        private void UpdateText(int value)
        {
            if (_amountText != null)
            {
                _amountText.text = value.ToString();
            }
        }
    }
}
