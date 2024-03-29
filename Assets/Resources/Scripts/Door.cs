﻿using UnityEngine;

public class Door : Obstruction, IInteractive
{
	Animator DoorAnimator = null;
	GvrAudioSource doorAudioSource = null;
	bool wasTransitioning = false;

	public AudioClip doorOpenCloseAudioClip;
	public AudioClip doorFinishOpenCloseAudioClip;
	public AudioClip doorLockedAudioClip;
	public bool IsDoorOpen = false;
	public bool IsLocked
	{
		get
		{
			return (UnlockSwitch != null && UnlockSwitch.IsLocked);
		}
	}
	public DoorUnlockSwitch UnlockSwitch { get; private set; }

	public override bool IsObstructing()
	{
		return !IsDoorOpen;
	}

	AbstractGameObject spaceTile = null;

	protected override void Awake()
	{
        base.Awake();
		DoorAnimator = GetComponent<Animator>();
		UnlockSwitch = null;
		doorAudioSource = TransformCached.GetComponentInChildren<GvrAudioSource>();
	}

	void Start()
	{
		spaceTile = LevelManager.CurrentLevel.GetGameObjectAtRowColumnIndex((int)(TransformCached.position.z / LevelManager.LevelScale), (int)(TransformCached.position.x / LevelManager.LevelScale));
	}

	void Update()
	{
		if (!DoorAnimator.IsPlaying())
		{
			if (wasTransitioning)
			{
				doorAudioSource.Stop();
				doorAudioSource.gainDb = 18.0f;
				doorAudioSource.PlayOneShot(doorFinishOpenCloseAudioClip);
			}
		}
		if (DoorAnimator.GetCurrentAnimatorStateInfo(0).IsName("DoorClosedStateAnimation"))
		{
			wasTransitioning = false;
		}
		else
		{
			wasTransitioning = DoorAnimator.IsPlaying();
		}

		if (spaceTile != null)
		{
            if (IsDoorOpen)
                spaceTile.tag = "SpaceTile";
            else
                spaceTile.tag = "WallTile";
		}
	}

	public void SetUnlockSwitch(DoorUnlockSwitch unlockSwitch)
	{
		UnlockSwitch = unlockSwitch;
	}

	public void ToggleDoorState()
	{
		if (!DoorAnimator.IsPlaying())
		{
			if (IsDoorOpen)
			{
                if (!LevelManager.CurrentLevel.SpaceOccupiedByPlayer(TransformCached.position))
                {
                    DoorAnimator.Play("DoorCloseAnimation");
                    doorAudioSource.gainDb = 6.0f;
                    doorAudioSource.PlayOneShot(doorOpenCloseAudioClip);
                }
			}
			else {
				if (!IsLocked)
				{
					DoorAnimator.Play("DoorOpenAnimation");
					doorAudioSource.gainDb = 6.0f;
					doorAudioSource.PlayOneShot(doorOpenCloseAudioClip);
				}
				else
				{
					doorAudioSource.gainDb = 8.0f;
					doorAudioSource.PlayOneShot(doorLockedAudioClip);
				}
			}
		}
	}

	public void Interact(Player sender)
	{
		ToggleDoorState();
	}
}
