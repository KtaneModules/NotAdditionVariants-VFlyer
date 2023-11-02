using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class SubtractionScript : MonoBehaviour {

	public KMAudio mAudio;
	public KMBombModule modSelf;
	public TextMesh mainDisplay, inputDisplay;
	public KMSelectable clrSelect, cycleSelect, subSelect;
	public KMSelectable[] digitsSelectable;

	int[] numbers;
	int moduleID, idxCycle;
	static int modIDCnt;
	int expectedValue;
	bool interactable = true, moduleSolved;

	const int cntNums = 10;
	const string digits = "0123456789";

	void QuickLog(string toLog, params object[] args)
	{
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
	}
	void QuickLogDebug(string toLog, params object[] args)
	{
		Debug.LogFormat("<{0} #{1}> {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
	}
	// Use this for initialization
	void Start () {
		moduleID = ++modIDCnt;
		ResetModule();
		for (var x = 0; x < digitsSelectable.Length; x++)
		{
			var y = x;
			digitsSelectable[x].OnInteract += delegate {
				if (interactable && !moduleSolved)
					HandleDigitPress(y);
				return false;
			};
		}
		subSelect.OnInteract += delegate {
			if (interactable && !moduleSolved)
				HandleSubmitPress();
			return false;
		};
		clrSelect.OnInteract += delegate {
			if (interactable && !moduleSolved)
			{
				clrSelect.AddInteractionPunch(.2f);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, clrSelect.transform);
				inputDisplay.text = "";
			}
			return false;
		};
		inputDisplay.text = "";
		cycleSelect.OnInteract += delegate {
			if (interactable && !moduleSolved)
			{
				cycleSelect.AddInteractionPunch(.2f);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, cycleSelect.transform);
				idxCycle = (idxCycle + 1) % cntNums;
				HandleCurCycle();
			}
			return false;
		};
	}
	void HandleCurCycle()
	{
		mainDisplay.text = numbers[idxCycle].ToString();
		mainDisplay.color = idxCycle == 0 ? Color.gray : Color.white;
		mainDisplay.characterSize = 0.002f + 0.001f * (idxCycle / 9f);
	}
	void ResetModule()
	{
		//numbers = Enumerable.Range(990, 10).ToArray();
		numbers = Enumerable.Range(100, 900).ToArray().Shuffle().Take(cntNums).ToArray();
		QuickLog("Displayed numbers: {0}", numbers.Join());
		expectedValue = numbers.First();
		for (var x = 1; x < cntNums; x++)
        {
			expectedValue -= numbers[x];
			if (expectedValue < 0)
				expectedValue *= -1;
		}
		QuickLog("Expected value to submit: {0}", expectedValue);
		HandleCurCycle();
	}
	void HandleDigitPress(int idx)
	{
		digitsSelectable[idx].AddInteractionPunch(.2f);
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, digitsSelectable[idx].transform);
		var curText = inputDisplay.text;
		if (curText.Length < 3)
			curText += idx.ToString();
		inputDisplay.text = curText;
	}
	void HandleSubmitPress()
	{
		subSelect.AddInteractionPunch(.2f);
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, subSelect.transform);
		var curText = inputDisplay.text;
		int subValue;
		if (int.TryParse(curText, out subValue) && subValue == expectedValue)
		{
			QuickLog("Expected value correctly submitted.");
			interactable = false;
			StartCoroutine(CorrectAnim());
		}
		else
		{
			QuickLog("Incorrect value submitted: \"{0}\" Starting over.", curText);
			modSelf.HandleStrike();
			ResetModule();
			inputDisplay.text = "";
		}
	}
	IEnumerator CorrectAnim()
	{
		mainDisplay.text = "CORRECT";
		mainDisplay.characterSize = 0.003f;
		mainDisplay.color = Color.white;
		yield return new WaitForSecondsRealtime(.5f);
		mainDisplay.text = "AND";
		yield return new WaitForSecondsRealtime(.5f);
		mainDisplay.text = "";
		inputDisplay.text = "";
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		modSelf.HandlePass();
		moduleSolved = true;
	}
	//twitch plays
#pragma warning disable 414
	private readonly string TwitchHelpMessage = "To cycle the numbers on the module, use \"!{0} cycle\". To adjust the cycle speed, append \"slow\",\"slower\",\"fast\", or \"faster\" to the cycle command. To submit a number to the module, use \"!{0} submit [number]\"";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand(string command)
	{
		var intCmd = command.Trim();
		var rgxMatchCycle = Regex.Match(intCmd, @"^cycle(\s(slow(er)?|fast(er)?))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		var rgxSubDigits = Regex.Match(intCmd, @"^submit\s[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (rgxMatchCycle.Success)
		{
			if (!interactable)
			{
				yield return "sendtochaterror You are not able to interact with the module right now. The command was not processed.";
				yield break;
			}
			var valCycle = rgxMatchCycle.Value.Split();
			var curDelay = 1f;
			if (valCycle.Length > 1)
			{
				switch (valCycle.Last())
				{
					case "slow": curDelay = 2f; break;
					case "slower": curDelay = 3f; break;
					case "fast": curDelay = 0.75f; break;
					case "faster": curDelay = 0.5f; break;
				}
			}

			yield return null;
			for (int x = 0; x < 10; x++)
			{
				yield return new WaitForSecondsRealtime(curDelay);
				cycleSelect.OnInteract();
			}
		}
		else if (rgxSubDigits.Success)
		{
			if (!interactable)
			{
				yield return "sendtochaterror You are not able to interact with the module right now. The command was not processed.";
				yield break;
			}
			var valueMatched = rgxSubDigits.Value.Split().Last();
			if (valueMatched.Length > 3)
			{
				yield return "sendtochaterror Number being sent has a length longer than 30. The answer can never contain more than 30 digits.";
				yield break;
			}
			yield return null;
			for (int x = 0; x < valueMatched.Length; x++)
			{
				digitsSelectable[digits.IndexOf(valueMatched[x])].OnInteract();
				yield return new WaitForSecondsRealtime(.1f);
			}
			subSelect.OnInteract();
			yield return "solve";
		}
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		string ans = expectedValue.ToString();
		for (int i = 0; i < inputDisplay.text.Length; i++)
		{
			if (inputDisplay.text[i] != ans[i])
			{
				clrSelect.OnInteract();
				yield return new WaitForSecondsRealtime(.1f);
				break;
			}
		}
		int start = inputDisplay.text.Length;
		for (int i = start; i < ans.Length; i++)
		{
			digitsSelectable[digits.IndexOf(ans[i])].OnInteract();
			yield return new WaitForSecondsRealtime(.1f);
		}
		subSelect.OnInteract();
		while (!moduleSolved) yield return true;
	}
}
