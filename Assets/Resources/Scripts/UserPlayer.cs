﻿using UnityEngine;

public class UserPlayer : Player
{
	const float footStepIntervalSeconds = 0.4f;
	const float footStepInitialOffset = footStepIntervalSeconds / 2.0f;

	GameObject redScreenOverlayObject = null;
	GvrAudioSource footStepsAudioSource = null;
	float lastFootStepElapsedTimeSeconds = footStepInitialOffset;
	bool redScreenOverlayFadingIn = true;

	protected override void Awake()
	{
		base.Awake();
		Transform redScreenOverlay = transform.FindBreadthFirst("RedScreenOverlay");
		if (redScreenOverlay != null)
		{
			redScreenOverlayObject = redScreenOverlay.gameObject;
		}
		footStepsAudioSource = transform.GetComponentInChildren<GvrAudioSource>();
	}

	void Start()
	{
		LevelManager.Initialize(gameObject, 2.0f);
		LevelManager.LoadLevel(0);
	}

	// Update is called once per frame
	protected override void Update()
	{
		base.Update();

		transform.position -= Velocity;

		Vector3 playerTileLocationUnrounded = transform.position / LevelManager.LevelScale;
		Vector3 playerTileLocation = new Vector3(Mathf.Round(playerTileLocationUnrounded.x), Mathf.Round(playerTileLocationUnrounded.y), Mathf.Round(playerTileLocationUnrounded.z));
		Vector3 xDirectionTileLocationUnrounded = playerTileLocation + Vector3.Project(Velocity, Vector3.right).normalized;
		Vector3 xDirectionTileLocation = new Vector3(xDirectionTileLocationUnrounded.x, xDirectionTileLocationUnrounded.y, xDirectionTileLocationUnrounded.z);
		object xDirectionTile = LevelManager.CurrentLevel.GetGameObjectAtRowColumnIndex((int)xDirectionTileLocation.z, (int)xDirectionTileLocation.x);
		Vector3 zDirectionTileLocationUnrounded = playerTileLocation + Vector3.Project(Velocity, Vector3.forward).normalized;
		Vector3 zDirectionTileLocation = new Vector3(zDirectionTileLocationUnrounded.x, zDirectionTileLocationUnrounded.y, zDirectionTileLocationUnrounded.z);
		object zDirectionTile = LevelManager.CurrentLevel.GetGameObjectAtRowColumnIndex((int)zDirectionTileLocation.z, (int)zDirectionTileLocation.x);

		if (xDirectionTile is GameObject && !(xDirectionTile as GameObject).CompareTag("SpaceTile") &&
			Vector3.Project((xDirectionTile as GameObject).transform.position - transform.position, Vector3.right).sqrMagnitude <=
			Mathf.Pow(LevelManager.LevelScale, 2))
		{
			GameObject xTileGO = xDirectionTile as GameObject;
			float repelX = (xTileGO.transform.position - (Vector3.Project(xTileGO.transform.position - transform.position, Vector3.right).normalized * LevelManager.LevelScale)).x;
			//transform.position = new Vector3(repelX, transform.position.y, transform.position.z);
			Velocity = Vector3.ProjectOnPlane(Velocity, Vector3.right);
		}

		if (zDirectionTile is GameObject && !(zDirectionTile as GameObject).CompareTag("SpaceTile") &&
			Vector3.Project((zDirectionTile as GameObject).transform.position - transform.position, Vector3.forward).sqrMagnitude <=
			Mathf.Pow(LevelManager.LevelScale, 2))
		{
			GameObject zTileGO = zDirectionTile as GameObject;
			float repelz = (zTileGO.transform.position - (Vector3.Project(zTileGO.transform.position - transform.position, Vector3.forward).normalized * LevelManager.LevelScale)).z;
			//transform.position = new Vector3(transform.position.x, transform.position.y, repelz);
			Velocity = Vector3.ProjectOnPlane(Velocity, Vector3.forward);
		}

		if ((Destination - transform.position).sqrMagnitude <= Mathf.Pow(0.725f * LevelManager.LevelScale, 2))
		{
			DestinationReached();
			lastFootStepElapsedTimeSeconds = footStepInitialOffset;
		}
		else
		{
			if (lastFootStepElapsedTimeSeconds >= footStepIntervalSeconds && !footStepsAudioSource.isPlaying)
			{
				footStepsAudioSource.Play();
				lastFootStepElapsedTimeSeconds = 0.0f;
			}
			lastFootStepElapsedTimeSeconds += Time.deltaTime;
		}

		transform.position += Velocity;

		if ((transform.position - LevelManager.CurrentLevel.GoalLocation).sqrMagnitude < 0.25f * LevelManager.LevelScale)
		{
			LevelManager.LoadNextLevel();
		}
	}

	public override void SetDestinationTarget(DestinationTarget destinationTarget)
	{
		Destination = transform.position;
		NextDestinations.Clear();
		Vector3 dest = destinationTarget.Destination;
		RaycastHit hit = new RaycastHit();
		if (Physics.Raycast(transform.position, transform.forward, out hit))
		{
			dest = new Vector3(hit.point.x, hit.transform.root.position.y, hit.point.z);
		}
	    NextDestinations.Push(dest);
		Target = destinationTarget.Target;
	}

	protected override void TargetReached()
	{
		Vector3 oldPosition = transform.position;
		base.TargetReached();
		transform.position = oldPosition;
	}

	/*
	void RedScreenOverlayFadeInAndOut()
	{
		Color col = redScreenOverlayObject.GetComponent<Renderer>().material.color;
		if (col.a >= 1.0f)
			redScreenOverlayFadingIn = false;
		else if (col.a <= 0.0f)
			redScreenOverlayFadingIn = true;
		float fadeSpeed = Time.deltaTime * (redScreenOverlayFadingIn ? 1.0f : -1.0f) * 0.5f;
		redScreenOverlayObject.GetComponent<Renderer>().material.color = new Color(col.r, col.g, col.b, 0.0f);
	}
	*/
}