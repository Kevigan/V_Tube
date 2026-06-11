// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mediapipe.Unity.Sample.UI
{
  public class ImageSourceConfig : ModalContents
  {
    public static string DebugState { get; private set; } = "ImageSourceConfig not initialized";

    private const string _SourceTypePath = "Scroll View/Viewport/Contents/SourceType/Dropdown";
    private const string _SourcePath = "Scroll View/Viewport/Contents/Source/Dropdown";
    private const string _ResolutionPath = "Scroll View/Viewport/Contents/Resolution/Dropdown";
    private const string _IsHorizontallyFlippedPath = "Scroll View/Viewport/Contents/IsHorizontallyFlipped/Toggle";

    private Dropdown _sourceTypeInput;
    private Dropdown _sourceInput;
    private Dropdown _resolutionInput;
    private Toggle _isHorizontallyFlippedInput;

    private bool _isChanged;
    private Coroutine _refreshSourceCoroutine;
    private Coroutine _waitForImageSourceCoroutine;

    public override void Exit()
    {
      GetModal().CloseAndResume(_isChanged);
    }

    public override void OnOpen()
    {
      DebugState = "ImageSourceConfig OnOpen";
      TryInitializeContents();
    }

    private void Start()
    {
      if (!gameObject.activeInHierarchy)
      {
        return;
      }

      TryInitializeContents();
    }

    private void TryInitializeContents()
    {
      if (ImageSourceProvider.ImageSource == null)
      {
        DebugState = "Waiting for ImageSourceProvider.ImageSource";
        if (_waitForImageSourceCoroutine == null)
        {
          _waitForImageSourceCoroutine = StartCoroutine(WaitForImageSourceAndInitialize());
        }
        return;
      }

      try
      {
        DebugState = "Initializing contents";
        Canvas.ForceUpdateCanvases();
        InitializeContents();
        DebugState = $"{DebugState}, InitializeContents complete";
      }
      catch (Exception ex)
      {
        DebugState = $"Init failed during [{DebugState}]: {ex.GetType().Name}: {ex.Message}";
        Debug.LogException(ex);
      }
    }

    private IEnumerator WaitForImageSourceAndInitialize()
    {
      const float timeoutSeconds = 5f;
      var elapsed = 0f;

      while (ImageSourceProvider.ImageSource == null && elapsed < timeoutSeconds)
      {
        elapsed += Time.unscaledDeltaTime;
        yield return null;
      }

      _waitForImageSourceCoroutine = null;

      if (ImageSourceProvider.ImageSource == null)
      {
        DebugState = "Timed out waiting for ImageSourceProvider.ImageSource";
        yield break;
      }

      DebugState = $"ImageSource ready: {ImageSourceProvider.ImageSource.GetType().Name}";
      TryInitializeContents();
    }

    private void InitializeContents()
    {
      DebugState = "InitializeSourceType";
      InitializeSourceType();
      DebugState = "InitializeSource";
      InitializeSource();
      DebugState = "InitializeResolution";
      InitializeResolution();
      DebugState = "InitializeIsHorizontallyFlipped";
      InitializeIsHorizontallyFlipped();
    }

    private void InitializeSourceType()
    {
      var sourceTypeTransform = gameObject.transform.Find(_SourceTypePath);
      if (sourceTypeTransform == null)
      {
        throw new NullReferenceException($"Missing transform at path '{_SourceTypePath}'");
      }
      _sourceTypeInput = sourceTypeTransform.gameObject.GetComponent<Dropdown>();
      if (_sourceTypeInput == null)
      {
        throw new NullReferenceException($"Missing Dropdown component at path '{_SourceTypePath}'");
      }
      _sourceTypeInput.ClearOptions();
      DebugState = "InitializeSourceType: cleared";
      _sourceTypeInput.onValueChanged.RemoveAllListeners();

      var options = Enum.GetNames(typeof(ImageSourceType)).Where(x => x != ImageSourceType.Unknown.ToString()).ToList();
      DebugState = $"InitializeSourceType: options={options.Count}";
      _sourceTypeInput.AddOptions(options);

      var currentSourceType = ImageSourceProvider.CurrentSourceType;
      DebugState = $"InitializeSourceType: current={currentSourceType}";
      var defaultValue = options.FindIndex(option => option == currentSourceType.ToString());

      if (defaultValue >= 0)
      {
        _sourceTypeInput.value = defaultValue;
      }
      _sourceTypeInput.RefreshShownValue();

      _sourceTypeInput.onValueChanged.AddListener(delegate
      {
        ImageSourceProvider.Switch((ImageSourceType)_sourceTypeInput.value);
        _isChanged = true;
        InitializeContents();
      });
    }

    private void InitializeSource()
    {
      var sourceTransform = gameObject.transform.Find(_SourcePath);
      if (sourceTransform == null)
      {
        throw new NullReferenceException($"Missing transform at path '{_SourcePath}'");
      }
      _sourceInput = sourceTransform.gameObject.GetComponent<Dropdown>();
      if (_sourceInput == null)
      {
        throw new NullReferenceException($"Missing Dropdown component at path '{_SourcePath}'");
      }
      _sourceInput.ClearOptions();
      DebugState = "InitializeSource: cleared";
      _sourceInput.onValueChanged.RemoveAllListeners();

      var imageSource = ImageSourceProvider.ImageSource;
      DebugState = $"InitializeSource: imageSource={(imageSource == null ? "<null>" : imageSource.GetType().Name)}";
      var sourceNames = imageSource.sourceCandidateNames;

      if (_refreshSourceCoroutine != null)
      {
        StopCoroutine(_refreshSourceCoroutine);
        _refreshSourceCoroutine = null;
      }

      DebugState = $"CurrentSourceType={ImageSourceProvider.CurrentSourceType}, CurrentSourceName={imageSource.sourceName ?? "<null>"}, CandidateCount={(sourceNames == null ? -1 : sourceNames.Length)}";

      if (sourceNames == null || sourceNames.Length == 0)
      {
        _sourceInput.ClearOptions();
        _sourceInput.AddOptions(new List<string> { "No cameras detected" });
        _sourceInput.value = 0;
        _sourceInput.RefreshShownValue();
        _sourceInput.interactable = false;
        DebugState += ", DropdownOptions=1, Interactable=false";
        if (imageSource is WebCamSource)
        {
          _refreshSourceCoroutine = StartCoroutine(RetryInitializeSource());
        }
        return;
      }

      _sourceInput.interactable = true;
      var options = new List<string>(sourceNames);
      DebugState = $"InitializeSource: candidateCount={options.Count}";
      _sourceInput.AddOptions(options);

      var currentSourceName = imageSource.sourceName;
      DebugState = $"InitializeSource: currentName={currentSourceName ?? "<null>"}";
      var defaultValue = options.FindIndex(option => option == currentSourceName);

      if (defaultValue < 0 && options.Count > 0)
      {
        DebugState = "InitializeSource: selecting default source 0";
        imageSource.SelectSource(0);
        currentSourceName = imageSource.sourceName;
        defaultValue = options.FindIndex(option => option == currentSourceName);
      }

      if (defaultValue >= 0)
      {
        _sourceInput.value = defaultValue;
      }
      else if (options.Count > 0)
      {
        _sourceInput.value = 0;
      }
      _sourceInput.RefreshShownValue();
      DebugState += $", DropdownOptions={_sourceInput.options.Count}, Value={_sourceInput.value}, Caption={_sourceInput.captionText?.text ?? "<null>"}, Interactable={_sourceInput.interactable}";

      _sourceInput.onValueChanged.AddListener(delegate
      {
        imageSource.SelectSource(_sourceInput.value);
        _isChanged = true;
        InitializeResolution();
      });
    }

    private IEnumerator RetryInitializeSource()
    {
      const float retryDelaySeconds = 0.5f;
      const int maxAttempts = 10;

      for (var attempt = 0; attempt < maxAttempts; attempt++)
      {
        yield return new WaitForSeconds(retryDelaySeconds);

        var sourceNames = ImageSourceProvider.ImageSource?.sourceCandidateNames;
        if (sourceNames != null && sourceNames.Length > 0)
        {
          _refreshSourceCoroutine = null;
          InitializeSource();
          InitializeResolution();
          yield break;
        }
      }

      _refreshSourceCoroutine = null;
    }

    private void InitializeResolution()
    {
      var resolutionTransform = gameObject.transform.Find(_ResolutionPath);
      if (resolutionTransform == null)
      {
        throw new NullReferenceException($"Missing transform at path '{_ResolutionPath}'");
      }
      _resolutionInput = resolutionTransform.gameObject.GetComponent<Dropdown>();
      if (_resolutionInput == null)
      {
        throw new NullReferenceException($"Missing Dropdown component at path '{_ResolutionPath}'");
      }
      _resolutionInput.ClearOptions();
      DebugState = "InitializeResolution: cleared";
      _resolutionInput.onValueChanged.RemoveAllListeners();

      var imageSource = ImageSourceProvider.ImageSource;
      DebugState = $"InitializeResolution: imageSource={(imageSource == null ? "<null>" : imageSource.GetType().Name)}";
      var resolutions = imageSource.availableResolutions;

      if (resolutions == null)
      {
        _resolutionInput.interactable = false;
        return;
      }

      _resolutionInput.interactable = true;
      var options = resolutions.Select(resolution => resolution.ToString()).ToList();
      DebugState = $"InitializeResolution: optionCount={options.Count}";
      _resolutionInput.AddOptions(options);

      var currentResolutionStr = imageSource.resolution.ToString();
      var defaultValue = options.FindIndex(option => option == currentResolutionStr);

      if (defaultValue >= 0)
      {
        _resolutionInput.value = defaultValue;
      }
      else if (options.Count > 0)
      {
        _resolutionInput.value = 0;
      }
      _resolutionInput.RefreshShownValue();

      _resolutionInput.onValueChanged.AddListener(delegate
      {
        imageSource.SelectResolution(_resolutionInput.value);
        _isChanged = true;
      });
    }

    private void InitializeIsHorizontallyFlipped()
    {
      var isHorizontallyFlippedTransform = gameObject.transform.Find(_IsHorizontallyFlippedPath);
      if (isHorizontallyFlippedTransform == null)
      {
        throw new NullReferenceException($"Missing transform at path '{_IsHorizontallyFlippedPath}'");
      }
      _isHorizontallyFlippedInput = isHorizontallyFlippedTransform.gameObject.GetComponent<Toggle>();
      if (_isHorizontallyFlippedInput == null)
      {
        throw new NullReferenceException($"Missing Toggle component at path '{_IsHorizontallyFlippedPath}'");
      }

      var imageSource = ImageSourceProvider.ImageSource;
      DebugState = $"InitializeIsHorizontallyFlipped: imageSource={(imageSource == null ? "<null>" : imageSource.GetType().Name)}";
      _isHorizontallyFlippedInput.isOn = imageSource.isHorizontallyFlipped;
      _isHorizontallyFlippedInput.onValueChanged.AddListener(delegate
      {
        imageSource.isHorizontallyFlipped = _isHorizontallyFlippedInput.isOn;
        _isChanged = true;
      });
    }
  }
}
