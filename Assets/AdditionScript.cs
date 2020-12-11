using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class AdditionScript : MonoBehaviour
{
	public KMAudio Audio;
    public KMBombInfo Bomb;
	public KMBombModule Module;
	
	public KMSelectable[] Keys, UtilityButtons;
	public TextMesh Cycle, Type;
	
	//Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;
	
	int[] Heckel = new int[10];
	int GuideNumber, Total;
	bool Interactable = true;
	
	void Awake()
    {
		moduleId = moduleIdCounter++;
		for (int a = 0; a < Keys.Count(); a++)
        {
            int Number = a;
            Keys[Number].OnInteract += delegate
            {
                PressNumber(Number);
				return false;
            };
        }
		
		for (int a = 0; a < UtilityButtons.Count(); a++)
        {
            int NumberU = a;
            UtilityButtons[NumberU].OnInteract += delegate
            {
                PressUtility(NumberU);
				return false;
            };
        }
	}
	
	void Start()
	{
		Genecode();
	}
	
	void Genecode()
	{
		Total = 0;
		GuideNumber = 0;
		string Log = "Numbers generated: ";
		for (int x = 0; x < 10; x++)
		{
			Heckel[x] = UnityEngine.Random.Range(100,1000);
			Total += Heckel[x];
			Log += x != 9 ? Heckel[x].ToString() + ", " : Heckel[x].ToString();
		}
		Debug.LogFormat("[Addition #{0}] {1}", moduleId, Log);
		Debug.LogFormat("[Addition #{0}] The total of the ten numbers: {1}", moduleId, Total.ToString());
		Cycle.text = Heckel[GuideNumber].ToString();
		Cycle.color = Color.gray;
	}
	
	void PressNumber(int Number)
	{
		Keys[Number].AddInteractionPunch(.2f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (Interactable)
		{
			if (Type.text.Length != 4)
			{
				Type.text += Number.ToString();
			}
		}
	}
	
	void PressUtility(int NumberU)
	{
		UtilityButtons[NumberU].AddInteractionPunch(.2f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (Interactable)
		{
			if (NumberU == 0)
			{
				Type.text = "";
			}
			
			else if (NumberU == 1)
			{
				if (Type.text == Total.ToString())
				{
					Debug.LogFormat("[Addition #{0}] You submitted {1}. That was correct. Module solved. ", moduleId, Type.text);
					Interactable = false;
					Cycle.color = Color.white;
					StartCoroutine(CorrectAnd());
				}
				
				else
				{
					if (Type.text.Length == 0)
					{
						Debug.LogFormat("[Addition #{0}] You submitted nothing. That was incorrect. Module striked. ", moduleId);
					}
					
					else
					{
						Debug.LogFormat("[Addition #{0}] You submitted {1}. That was incorrect. Module striked. ", moduleId, Type.text);
					}
					Module.HandleStrike();
					Genecode();
					Type.text = "";
				}
			}
			
			else if (NumberU == 2)
			{
				GuideNumber = (GuideNumber + 1) % 10;
				Cycle.text = Heckel[GuideNumber].ToString();
				Cycle.color = GuideNumber == 0 ? Color.gray : Color.white;
			}
		}
	}
	
	IEnumerator CorrectAnd()
	{
		Cycle.text = "CORRECT";
		yield return new WaitForSecondsRealtime(.5f);
		Cycle.text = "AND";
		yield return new WaitForSecondsRealtime(.5f);
		Cycle.text = "";
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		Module.HandlePass();
	}
	
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To cycle the numbers on the module, use !{0} cycle | To submit a number to the module, use !{0} submit [number]";
    #pragma warning restore 414
	
	IEnumerator ProcessTwitchCommand(string command)
    {
		string[] parameters = command.Split(' ');
		if (Regex.IsMatch(command, @"^\s*cycle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
			if (!Interactable)
			{
				yield return "sendtochaterror You are not able to interact with the module. The command was not processed.";
				yield break;
			}
			
			for (int x = 0; x < 10; x++)
			{
				yield return new WaitForSecondsRealtime(1f);
				UtilityButtons[2].OnInteract();
			}
        }
		
		if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
			if (!Interactable)
			{
				yield return "sendtochaterror You are not able to interact with the module. The command was not processed.";
				yield break;
			}
			
			if (parameters.Length != 2)
			{
				yield return "sendtochaterror Invalid parameter length. The command was not processed.";
				yield break;
			}
			
			int Hecks;
			bool Check = Int32.TryParse(parameters[1], out Hecks);
			
			if (!Check)
			{
				yield return "sendtochaterror Number being sent is invalid. The command was not processed.";
				yield break;
			}
			
			if (parameters[1].Length > 4)
			{
				yield return "sendtochaterror Number being sent has a length longer than 4. The command was not processed.";
				yield break;
			}
			
			for (int x = 0; x < parameters[1].Length; x++)
			{
				Keys[Int32.Parse(parameters[1][x].ToString())].OnInteract();
				yield return new WaitForSecondsRealtime(.25f);
			}
			
			yield return "solve";
			UtilityButtons[1].OnInteract();
        }
	}
}