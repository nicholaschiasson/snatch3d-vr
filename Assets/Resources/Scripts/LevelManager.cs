﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

public static class LevelManager
{

	public static GameObject PlayerGameObject { get; private set; }
	public static int LevelNumber { get; private set; }
	public static Level CurrentLevel { get; private set; }

	public static float LevelScale
	{
		get
		{
			return CurrentLevel.LevelScale;
		}
	}

	public static void Initialize(GameObject playerGameObject, float levelScale)
	{
		if (PlayerGameObject == null)
		{
			PlayerGameObject = playerGameObject;
			LevelNumber = 0;
			CurrentLevel = new Level(levelScale);
		}
	}

	public static void LoadLevel(int level)
	{
		LevelNumber = level;
		/* Path.Combine with string array parameter is unsupported for some reason on Mac OS X */
		/* Using custom Path.Combine workaround */
		string levelDesignPath = Utils.Path.Combine("LevelDesigns", "Level" + level);
		TextAsset levelJSON = Resources.Load<TextAsset>(levelDesignPath);
		if (levelJSON == null)
		{
			if (level != 0)
			{
				LoadLevel(0);
			}
			return;
		}
		JSONNode levelDesign = JSON.Parse(levelJSON.text);
		bool playerStartSpace = false;

		CurrentLevel.Destroy();

		// Reading the JSON level.Grid string array field
		int i = 0;
		foreach (var row in levelDesign["Grid"].AsArray)
		{
			int rowIndex = (levelDesign["Grid"].Count - 1) - i;
			CurrentLevel.AddRow(new List<AbstractGameObject>());

			foreach (var column in row.ToString().Trim('"').ToCharArray().Select((value, index) => new { value, index }))
			{
				string assetPrefabPath = Utils.Path.Combine("Prefabs");
				string assetPath = null;
				Quaternion doorRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
				Vector3 rowColumnPosition = new Vector3(column.index * LevelScale, 0.0f, rowIndex * LevelScale);

				switch (column.value)
				{
					case 'W':
						// Wall
						assetPath = Utils.Path.Combine(assetPrefabPath, "WallTiles", "BrickWallTile");
						GameObject wall = MonoBehaviour.Instantiate(Resources.Load(assetPath)) as GameObject;
                        WallTile wallScript = wall.GetComponent<WallTile>();
						wallScript.TransformCached.position = rowColumnPosition;
						wallScript.TransformCached.localScale *= LevelScale;
						CurrentLevel.AddToRow(i, wallScript);
						break;
					case 'd':
						// Door left-right
						doorRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
						goto case 'D';
					case 'D':
						// Door forward-backward
						assetPath = Utils.Path.Combine(assetPrefabPath, "Doors", "WoodenDoor");
						GameObject door = MonoBehaviour.Instantiate(Resources.Load(assetPath)) as GameObject;
                        Door doorScript = door.GetComponent<Door>();
                        doorScript.TransformCached.position = rowColumnPosition;
                        doorScript.TransformCached.rotation = doorRotation;
                        doorScript.TransformCached.localScale *= LevelScale;
						CurrentLevel.LevelEnvironmentObjects.Add(doorScript);
						CurrentLevel.ObstructionMap[rowColumnPosition] = doorScript;
						goto case 'F';
					case '^':
						// Does not work with Google VR SDK
						// Start Facing Forward
						if (!playerStartSpace)
						{
                            Player.MainPlayer.TransformCached.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
						}
						goto case 'S';
					case 'v':
						// Does not work with Google VR SDK
						// Start Facing Backward
						if (!playerStartSpace)
						{
                            Player.MainPlayer.TransformCached.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
						}
						goto case 'S';
					case '<':
						// Does not work with Google VR SDK
						// Start Facing Left
						if (!playerStartSpace)
						{
                            Player.MainPlayer.TransformCached.rotation = Quaternion.Euler(0.0f, -90.0f, 0.0f);
						}
						goto case 'S';
					case '>':
						// Does not work with Google VR SDK
						// Start Facing Right
						if (!playerStartSpace)
						{
                            Player.MainPlayer.TransformCached.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
						}
						goto case 'S';
					case 'S':
						// Start
						if (!playerStartSpace)
						{
                            Player.MainPlayer.SetPosition(rowColumnPosition);
							playerStartSpace = true;
						}
						goto case 'F';
					case 'G':
						// Goal
						assetPath = Utils.Path.Combine(assetPrefabPath, "Effects", "GoalSpaceTileEffect");
						GameObject goal = MonoBehaviour.Instantiate(Resources.Load(assetPath)) as GameObject;
                        GoalEffect goalScript = goal.GetComponent<GoalEffect>();
                        goalScript.TransformCached.position = rowColumnPosition;
                        goalScript.TransformCached.localScale *= LevelScale;
						CurrentLevel.LevelEnvironmentObjects.Add(goalScript);
						CurrentLevel.GoalLocation = rowColumnPosition;
						goto case 'F';
					case 'F':
						// Floor
						assetPath = Utils.Path.Combine(assetPrefabPath, "SpaceTiles", "ConcreteFloorTiledCeilingSpaceTile");
						GameObject floor = MonoBehaviour.Instantiate(Resources.Load(assetPath)) as GameObject;
                        SpaceTile floorScript = floor.GetComponent<SpaceTile>();
						floorScript.TransformCached.position = rowColumnPosition;
                        floorScript.TransformCached.localScale *= LevelScale;
						CurrentLevel.AddToRow(i, floorScript);
						break;
					default:
						break;
				}
			}
			i++;
		}

		// Reading the JSON level.LightSourceMap string array field
		i = 0;
		foreach (var row in levelDesign["LightSourceMap"].AsArray)
		{
			foreach (var column in row.ToString().Trim('"').ToCharArray().Select((value, index) => new { value, index }))
			{
				if (column.value == '-')
				{
					continue;
				}
				if (!CurrentLevel.LightSourceListenerMap.ContainsKey(column.value))
				{
					CurrentLevel.LightSourceListenerMap[column.value] = new List<ILightSourceListener>();
				}
				var spaceTile = CurrentLevel.LevelGrid[i][column.index] as SpaceTile;
				spaceTile.SetLightSource(column.value);
				CurrentLevel.LightSourceListenerMap[column.value].Add(spaceTile);
			}
			i++;
		}

		// Reading the JSON level.LightSwitches object array field
		foreach (JSONNode lightSwitch in levelDesign["LightSwitches"].AsArray)
		{
			string assetPath = Utils.Path.Combine("Prefabs", "Switches", "LightSwitch");
			GameObject light = MonoBehaviour.Instantiate(Resources.Load(assetPath)) as GameObject;
            LightSwitch lightScript = light.GetComponent<LightSwitch>();
            lightScript.TransformCached.position = new Vector3(lightSwitch["Position"]["X"].AsFloat * LevelScale, 0.0f, lightSwitch["Position"]["Y"].AsFloat * LevelScale);
            lightScript.TransformCached.rotation = Quaternion.Euler(0.0f, lightSwitch["Yaw"].AsFloat, 0.0f);
            lightScript.TransformCached.localScale *= LevelScale;
            lightScript.SetLightSource(lightSwitch["LightSource"].ToString().Trim('"').ToCharArray()[0]);
			CurrentLevel.LightSourceMap[lightSwitch["LightSource"].ToString().Trim('"').ToCharArray()[0]] = lightScript;
			CurrentLevel.LevelEnvironmentObjects.Add(lightScript);
		}

		// Reading the JSON level.DoorUnlockSwitches object array field
		foreach (JSONNode doorUnlockSwitch in levelDesign["DoorUnlockSwitches"].AsArray)
		{
			string assetPath = Utils.Path.Combine("Prefabs", "Switches", "DoorUnlockSwitch");
			GameObject doorSwitch = MonoBehaviour.Instantiate(Resources.Load(assetPath)) as GameObject;
            DoorUnlockSwitch doorSwitchScript = doorSwitch.GetComponent<DoorUnlockSwitch>();
            doorSwitchScript.TransformCached.position = new Vector3(doorUnlockSwitch["Position"]["X"].AsFloat * LevelScale, 0.0f, doorUnlockSwitch["Position"]["Y"].AsFloat * LevelScale);
            doorSwitchScript.TransformCached.rotation = Quaternion.Euler(0.0f, doorUnlockSwitch["Yaw"].AsFloat, 0.0f);
            doorSwitchScript.TransformCached.localScale *= LevelScale;
			foreach (JSONNode unlockableDoor in doorUnlockSwitch["DoorPositions"].AsArray)
			{
				Vector3 doorPos = new Vector3(unlockableDoor["X"].AsFloat * LevelScale, 0.0f, unlockableDoor["Y"].AsFloat * LevelScale);
				if (CurrentLevel.ObstructionMap.ContainsKey(doorPos) && CurrentLevel.ObstructionMap[doorPos] is Door)
				{
					(CurrentLevel.ObstructionMap[doorPos] as Door).SetUnlockSwitch(doorSwitchScript);
				}
			}
			CurrentLevel.LevelEnvironmentObjects.Add(doorSwitchScript);
		}

		// Reading the JSON level.Enemies object array field
		foreach (JSONNode enemy in levelDesign["Enemies"].AsArray)
		{
			string assetPath = Utils.Path.Combine("Prefabs", "Enemies", enemy["Type"]);
			GameObject enemyPlayer = MonoBehaviour.Instantiate(Resources.Load(assetPath)) as GameObject;
            EnemyPlayer enemyScript = enemyPlayer.GetComponent<EnemyPlayer>();
            enemyScript.SetPosition(new Vector3(enemy["Position"]["X"].AsFloat * LevelScale, 0.0f, enemy["Position"]["Y"].AsFloat * LevelScale));
            enemyScript.TransformCached.position = new Vector3(enemy["Position"]["X"].AsFloat * LevelScale, 0.0f, enemy["Position"]["Y"].AsFloat * LevelScale);
            enemyScript.TransformCached.rotation = Quaternion.Euler(0.0f, enemy["Yaw"].AsFloat, 0.0f);
            enemyScript.TransformCached.localScale *= LevelScale;
            enemyScript.SetRestTimeSeconds(enemy["RestTimeSeconds"].AsFloat);
			List<Vector3> patrolPath = new List<Vector3>();
			foreach (JSONNode position in enemy["PatrolPath"].AsArray)
			{
				patrolPath.Add(new Vector3(position["X"].AsFloat * LevelScale, 0.0f, position["Y"].AsFloat * LevelScale));
			}
            enemyScript.SetPatrolPath(patrolPath);
			string enemyLightSource = enemy["LightSource"].ToString().Trim('"');
			if (!string.IsNullOrEmpty(enemyLightSource) && enemyLightSource != "-")
			{
				char enemyLightSourceChar = enemyLightSource.ToCharArray()[0];
				if (!CurrentLevel.LightSourceListenerMap.ContainsKey(enemyLightSourceChar))
				{
					CurrentLevel.LightSourceListenerMap[enemyLightSourceChar] = new List<ILightSourceListener>();
				}
                enemyScript.SetLightSource(enemyLightSourceChar);
				CurrentLevel.LightSourceListenerMap[enemyLightSourceChar].Add(enemyScript);
			}
			CurrentLevel.LevelEnemies.Add(enemyScript);
		}

		if (!playerStartSpace)
		{
			Debug.LogException(new System.Exception("No player start position."));
			Application.Quit();
		}

		Player.Initialize();
	}

	public static void LoadNextLevel()
	{
		LoadLevel(LevelNumber + 1);
	}

	public static Vector3 LevelGridCoords(Vector3 coords)
	{
		Vector3 unscaledCoords = coords / LevelScale;
		return new Vector3(Mathf.Round(unscaledCoords.x), Mathf.Round(unscaledCoords.y), Mathf.Round(unscaledCoords.z)) * LevelScale;
	}
}

