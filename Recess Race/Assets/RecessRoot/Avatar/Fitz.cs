using UnityEngine;
using System.Collections;

public class Fitz : Platformer {
	
	/*public float wSpeed;
		public float rSpeed;
		public float sSpeed;
		public float accel;
		public float decel;
		public float skidDecel;
		public float rSkidDecel;
		
		public float jHeight;
		public float jExtraHeight;
		public float airSpeedInit;
		public float airSpeedExtra = 0.15625f;
		public float runJumpIncrement = 0.5f;
		public float airAccel;
		public float fallSpeedMax;
		public float gravity;
		public float gravityPlus;*/
	
	public MovementVariables mondo = new MovementVariables
		(
			1.25f, 				//wSpeed
			2.25f,  			//rSpeed
			3f,  				//sSpeed
			0.09375f,  			//accel
			0.0625f,  			//decel
			0.15625f,  			//skidDecel
			0.3125f,  			//running SkidDecel
			3.0f,  				//jHeight
			5.5f,  				//jExtraHeight
			1.25f,  			//airSpeedH
			4.8125f,  			//airSpeedInit
			4f,  				//fallSpeedMax
			0.1875f,  			//gravity
			0.375f 				//gravityPlus
		);
	
	
	public MovementVariables pinky = new MovementVariables
		(
			1.3125f,  			//wSpeed
			1.4375f,  			//rSpeed
			2f,  				//sSpeed
			0.5f,  				//accel
			0.1875f,  			//decel
			0.03125f,			//air decel
			1.3125f,  			//float speed
			5.5f, 				//height of jump with Jump pressed
			2.5f,  				//floating air speed init
			1.4375f,  			//airSpeedH
			4f,  				//airSpeedInit
			4f,  				//fallSpeedMax
			0.15625f,  			//gravity
			0.01625f			//gravity for floating
		);
	
	
	public MovementVariables boogerBoy = new MovementVariables
		(
			1.3125f,  			//wSpeed
			1.4375f,  			//rSpeed
			2f,  				//sSpeed
			0.5f,  				//accel
			0.1875f,  			//decel
			0.03125f,			//air decel
			1.3125f,  			//float speed
			64f,  				//jHeight
			0.0f,  				//jExtraHeight
			1.4375f,  			//airSpeedH
			4f,  				//airSpeedInit
			4f,  				//fallSpeedMax
			0.15625f,  			//gravity
			0.01625f			//gravity for floating
		);
	
	public MovementVariables currentMotor = null;
	
	public float CurMaxSpeed{
		get { 
				if (currentMotor == mondo){
					return sprinting? currentMotor.sSpeed : (controller.getRun? currentMotor.rSpeed : currentMotor.wSpeed); 
				}
				else if (currentMotor == pinky){
					return sprinting || charging? (falling && !charging? pinky.rSpeed : (agape? pinky.rSpeed : pinky.sSpeed)) : (jumping? pinky.rSpeed : pinky.wSpeed);
				}
				else if (currentMotor == boogerBoy){
					return dashing? currentMotor.sSpeed : currentMotor.wSpeed;
				}
				else if (currentMotor == jumpman){
					return jumpman.wSpeed;
				}
				else{
					Debug.LogWarning("You're trying to get a max speed but I don't like the look of you");
					return jumpman.wSpeed;
				}
			
			}
	}
	public float SkidDecel{
		get { return controller.getRun? currentMotor.decelB : currentMotor.decelA; }
	}
	
	public float PinkyDecel{
		get { return crouching ? currentMotor.decelA : currentMotor.decel; }
	}
	
	public float CurGravity{
		get { return controller.getJump? currentMotor.gravity : currentMotor.gravityA; }
	}
	
	public float FallSpeed{
		get { return fallingSlowly? currentMotor.decelB : currentMotor.fallSpeedMax;}
	}
	
	public float PinkyGravity{
		get { return fallingSlowly? currentMotor.gravityA : currentMotor.gravity; }
	}
	
	public string PinkyWalk {
		get { return mouthFull? a_walkBig : a_walk; }
	}
	
	public string PinkyJump {
		get { return mouthFull? a_jumpBig : a_jump; }
	}
	
	public string PinkyRun {
		get { return mouthFull? a_runBig : a_run; }
	}
	
	public string PinkyIdle { 
		get { return mouthFull? a_idleBig : a_idle; }
	}
	
	public string PinkyStop {
		get { return mouthFull? a_jumpBig : a_stop; }
	}
	
	//mondo specific other-variables:
	private bool sprinting = false;
	private bool fallingSlowly = false;
	private int pMeter = 0;
	private Transform ballHolding;
	private int ballOffset = 16;
	
	private bool crouching = false;
	
	
	//pinky specific other-variables
	
	private bool exhaling = false;
	private bool willCloseMouth = false;
	private bool agape = false;
	private Vector2 hurtVector = new Vector2(1.5f, 3.5f);
	private bool meteor = false;
	private bool mouthFull = false;
	private GameObject inMouth;
	private bool swallowing = false;
	private float busyTiming = 0.5f;
	private float busyTimer;
	private bool busy;
	private bool Busy {
		get { return busy; }
		set { busy = value;
			busyTimer = busy? busyTiming : 0;
		}
	}
	private bool isDoc = false;
	private bool IsDoc{
		get { return isDoc; }
		set {
			isDoc = value;
			charging = false;
			int scale = (int) anim.transform.localScale.x;
			Destroy(dummy);
			dummy = Instantiate(Resources.Load("pinkyDocDummy"), t.position - Vector3.forward, t.rotation) as GameObject;
			anim = dummy.GetComponent<tk2dSpriteAnimator>();
			sprite = dummy.GetComponent<tk2dSprite>();
			sprite.transform.localScale = new Vector3(scale, 1, 1);
			dummy.transform.parent = t;
			FitDetectors();
		}
	}
	private bool sliding = false;
	private bool Sliding {
		get { return sliding; }
		set {
			sliding = value;
			if (value){
				anim.Play (a_dash);
				velocity = new Vector2(currentMotor.sSpeed * FacingRightMod, 0);
			}
			else{
				Debug.Log ("Sliding? PFF");
				if (!controller.getD){
					PinkyDownUp();
				}
				else{
					anim.Play (a_crouch);
				}
			}
		}
	}
	private bool charging = false;
	private Vector2 wallHitVelocity = new Vector2(1.35f, 4.0f);
	
	
	public bool FacingRight { 
		get { return sprite.transform.localScale.x > 0.9f; }
	}
	
	public int FacingRightMod {
		get { return FacingRight? 1 : -1; }
	}
	
	//animation variables
	
	
	private string a_walk = "walk";
	private string a_idle = "idle";
	private string a_fall = "fall";
	private string a_jump = "jump";
	private string a_sJump = "sprintJump";
	private string a_land = "land";
	private string a_dash = "dash";
	private string a_wallGrab = "wallGrab";
	private string a_wallJump = "wallJump";
	private string a_skid = "skid";
	private string a_float = "float";
	private string a_slide = "slide";
	private string a_stop = "stop";
	private string a_run = "run";
	private string a_exhale = "exhale";
	private string a_agape = "agape";
	private string a_hurt = "hurt";
	private string a_crouch = "crouch";
	
	//Pinky big anims
	private string a_swallow = "swallow";
	private string a_walkBig = "walkBig";
	private string a_runBig = "runBig";
	private string a_jumpBig = "jumpBig";
	private string a_hurtBig = "hurtBig";
	private string a_idleBig = "idleBig";
	
	//pinky doc anims
	private string a_curl = "curl";
	private string a_charge = "charge";
	private string a_uncurl = "uncurl";
	
	//booger boy anims
	private string a_endDash = "endDash";
		
	
	public static Fitz fitz;
	
	
	void Awake(){
		if (fitz == null){
			fitz = this;
		}
		else{
			Destroy(this.gameObject);
		}
	}
	
	// Use this for initialization
	void Start () {
		
		Setup();
		Application.targetFrameRate = 60;
		//TEMP
		ChangeToBoogerBoy();
	}
	
	// Update is called once per frame
	void Update () {
		
		FitDetectors();
		
		ApplyMovement();
		
		controller.GetInputs();
		
		CheckStates();
		
		if (currentMotor == mondo){			//play the update of whoever I'm powered up as
			MondoUpdate();
								//TODO : make a real switching mechanism. Pressing 1 and 2 or whatever is shitty and needs to change
			
		}
		else if (currentMotor == pinky){
			PinkyUpdate();
		}
		else if (currentMotor != boogerBoy){
			JumpmanUpdate();
			
		}
		
		if (Input.GetKeyDown(KeyCode.Alpha2)){
			ChangeToPinky();
		}
		
		if (Input.GetKeyDown(KeyCode.Alpha1)){
			ChangeToMondo();
		}
		Cleanup();
	}
	
	public override void NothingBottom (){
		base.NothingBottom ();
		if (sliding){
			Sliding = false;
			if (currentMotor == pinky)
				RunUp();
		}
	}
	
	public override void DetectorEnter (BoxCollider detector, BoxCollider colEntering)
	{
		base.DetectorEnter (detector, colEntering);
		Doc docScript = colEntering.GetComponent<Doc>();
		
		if (docScript){				//what happens when what I hit is Doc?
			
			if(currentMotor == mondo){		//Am I mario or what
				
				if (velocity.y > 0 && docScript.Dangerous){
					Debug.Log("Get hurt!");
				}
				
				else if (detector == botDetector && docScript.JumpedOn(controller.getRun) && velocity.y < 0){		//where did I hit? Do I jump? If I jump, do I rebound (JumpedOn gives me this result; see Doc.cs)
					velocity = new Vector2(velocity.x, controller.getJump? mondo.jExtraHeight : mondo.jHeight);
				}
				else if ((detector == rightDetector || detector == leftDetector)){
					if (docScript.Dangerous){			//should the shell hurt me?
						Debug.Log("Get hurt!");
						RecessManager.Instance.Death();
					}
					else if (!controller.getRun){ 		//should I kick the shell?
						docScript.Kicked();
						pMeter = 0;
						velocity = new Vector2(0, velocity.y);
					}
					else {				//or I guess I should hold on to it
						ballHolding = docScript.transform;
						docScript.Held = true;
						Debug.Log("Hold that ball");
					}
				}
				else if (!docScript.Dangerous && controller.getRun){
					ballHolding = docScript.transform;
					docScript.Held = true;
					Debug.Log("Holding ball now");
				}
			}
			else if (currentMotor == pinky){
				
				//TODO : hurt Doc; he always gets hurt if I touch him, unless I'm spitting him out.
				if ((detector == topDetector || detector == botDetector) && !meteor){
					//TODO: Hurt self because I'm not going fast enough to be a weapon
					RecessManager.Instance.Death();
					return;
				}
				
				if (((detector == rightDetector && FacingRight) || (detector == leftDetector && !FacingRight)) && agape){
					docScript.Inhaled();
					mouthFull = true;
					inMouth = docScript.gameObject;
					agape = false;
					anim.Play(PinkyIdle);
					
				}
			}
		} 		//end if docscript
		else if (detector == botDetector){
			
			Checkpoint checkScript = colEntering.GetComponent<Checkpoint>();
			if (checkScript != null){
				checkScript.Enter ();
				Debug.Log ("There's a checkpoint script.");
			}
		}
		
		if (colEntering.gameObject.layer == LayerMask.NameToLayer("normalCollision") && (detector == leftDetector || detector == rightDetector) && charging){
			EndCharge ();
		}
		int layer = colEntering.gameObject.layer;
		if (layer == 31 && colEntering.tag == "spike"){
			Debug.Log ("OUCH");
			RecessManager.Instance.Death ();
		}
		
	}
	
	
	private void MondoUpdate(){
		
		//TODO This should be put in an event to save on checking its stuff each frame. MEH
		if (pMeter >= 112 && grounded && ((velocity.x <= -mondo.rSpeed && controller.getL) || (velocity.x >= mondo.rSpeed && controller.getR))){ //check to see if I'm sprinting! (pMeter)
			pMeter = 112;	//I need to be on the ground, and I have to be pressing forward in the direction I'm running
			sprinting = true;
			if (!anim.IsPlaying(a_dash)){
				anim.Play(a_dash);
				velocity = new Vector2((velocity.x > 0? mondo.sSpeed : -mondo.sSpeed), velocity.y);
				
			}
		}
		else if (grounded) {
			sprinting = false;
			if(!anim.IsPlaying(a_walk) && velocity.x != 0){
				anim.Play(a_walk);
			}
		}
		
		//end sprint
		
		if (controller.getL && canMoveLeft){									//if I'm going left...
			if (velocity.x > 0){
			//	Debug.Log(velocity + "before skid");
				velocity += new Vector2(-SkidDecel, 0);		
			//	Debug.Log(velocity + "after skid");
				if (!anim.IsPlaying(a_skid) && grounded){
					anim.Play(a_skid);
					sprinting = false;
				}
			}
			else{
				velocity += new Vector2(-mondo.accel, 0);
				if (!anim.IsPlaying(a_walk) && grounded && !sprinting){
					anim.Play(a_walk);
				}
			}
		}
		else if (controller.getR && canMoveRight) {							//if I'm going right... in this instance having getL and getR both be true should be impossible,
			if (velocity.x < 0){																							//but getL takes priority nonetheless
				velocity += new Vector2(SkidDecel, 0);
				if (!anim.IsPlaying(a_skid) && grounded){
					anim.Play(a_skid);
					sprinting = false;
				}
			}
			else{
				velocity += new Vector2(mondo.accel, 0);
				if (!anim.IsPlaying(a_walk) && grounded && !sprinting){
					anim.Play(a_walk);
				}
			}
		}
		else if (!controller.getL && !controller.getR){		//if I'm not going either direction, apply friction (but not if I'm in air)
			if (velocity.x > mondo.decel) {
				velocity += new Vector2(-mondo.decel, 0);
			}
			else if (velocity.x < -mondo.decel) {
				velocity += new Vector2(mondo.decel, 0);
			}
			
			if (velocity.x <= mondo.decel && velocity.x >= -mondo.decel && grounded){
				velocity = new Vector2(0, velocity.y);
				anim.Play(a_idle);
			}
		}
		//end L/R inputs
		
		if (velocity.x > CurMaxSpeed){							//correct my velocity according to my max, and add to the P-meter if applicable
			velocity = new Vector2(CurMaxSpeed, velocity.y);
		}
		
		if (velocity.x < -CurMaxSpeed){
			velocity = new Vector2(-CurMaxSpeed, velocity.y);
		}
		
		if (velocity.x == mondo.rSpeed || velocity.x == -mondo.rSpeed && pMeter < 112 && grounded){
			pMeter += 2;
		}	
		else if (velocity.x < mondo.rSpeed && velocity.x > -mondo.rSpeed && pMeter > 0){
			pMeter --;
		}
		
		
		if (!grounded){											//Apply gravity!
			if (velocity.y > -mondo.fallSpeedMax)
				velocity += new Vector2(0, -CurGravity);
			else if (velocity.y < -mondo.fallSpeedMax)
				velocity = new Vector2(velocity.x, -mondo.fallSpeedMax);
		}
		
		if (velocity.y < 0){
			falling = true;
			jumping = false;
			if (!anim.IsPlaying(a_fall) && !sprinting){
				anim.Play(a_fall);
			}
		}
		
		if (ballHolding){
			ballHolding.position = FacingRight? new Vector3(t.position.x + ballOffset, t.position.y, ballHolding.position.z) : new Vector3(t.position.x - ballOffset, t.position.y, ballHolding.position.z);
			if (controller.getRunUp){
				ballHolding.SendMessage("Kicked", velocity.x);
				ballHolding = null;
			}
		}
		meteor = Mathf.Abs(velocity.y) > 3.0f;
	}
	
	private void PinkyUpdate(){
		
		if (busy){
			busyTimer -= Time.deltaTime;
			if (busyTimer <= 0){
				busy = false;
				busyTimer = 0;
				if (agape && willCloseMouth){
					agape = false;
					willCloseMouth = false;
				}
			}
		}
		if (charging) goto capSpeed;
		if (controller.getL && canMoveLeft && !agape && !crouching && !swallowing){									//if I'm going left...
			if (velocity.x > -CurMaxSpeed)
				velocity += new Vector2(-pinky.accel, 0);
			if (!anim.IsPlaying(PinkyWalk) && grounded && !sprinting && !exhaling){
				anim.Play(PinkyWalk);
			}
			
		}
		else if (controller.getR && canMoveRight && !agape && !crouching && !swallowing) {							//if I'm going right... in this instance having getL and getR both be true should be impossible,
			if (velocity.x < CurMaxSpeed)
				velocity += new Vector2(pinky.accel, 0);
			if (!anim.IsPlaying(PinkyWalk) && grounded && !sprinting && !exhaling){
				anim.Play(PinkyWalk);
			}
			
		}
		else if ((!controller.getL && !controller.getR) || agape || crouching){		//if I'm not going either direction, apply friction (but not if I'm in air)
			if (velocity.x > PinkyDecel) {
				velocity += new Vector2(-(grounded? PinkyDecel : pinky.decelA), 0);
			}
				
			else if (velocity.x < PinkyDecel) {
				velocity += new Vector2(grounded? PinkyDecel : pinky.decelA, 0);
			}
			
			if (velocity.x <= PinkyDecel && velocity.x >= -PinkyDecel){
				velocity = new Vector2(0, velocity.y);
				if (grounded && !exhaling && !agape && !crouching && !swallowing)
					anim.Play(PinkyIdle);
				if (sliding){
					Sliding = false;
				}
			}
			else if (grounded && !swallowing){
				if (!anim.IsPlaying(PinkyStop) && !exhaling && !agape && !crouching){
					anim.Play(PinkyStop);
				}
			}
		}
		
		if (controller.doubleTap && grounded && !agape && !swallowing){
			sprinting = true;
			anim.Play(PinkyRun);
		}
		
	capSpeed:
		
		if (velocity.x > CurMaxSpeed){
			velocity = new Vector2(Mathf.Max(CurMaxSpeed, velocity.x - pinky.decel), velocity.y);
		}
		
		if (velocity.x < -CurMaxSpeed){
			velocity = new Vector2(Mathf.Min(-CurMaxSpeed, velocity.x + pinky.decel), velocity.y);
		}
		
	stopSprint:
		if ((controller.getLUp || controller.getRUp) && sprinting && grounded){
			sprinting = false;
		}
		
		
		
						//Do the Float Thing
		
		
		
	gravity:
		if (!grounded){											//Apply gravity!
			if (velocity.y > -FallSpeed)
				velocity += new Vector2(0, -pinky.gravity);
			else if (velocity.y < -FallSpeed)
				velocity = new Vector2(velocity.x, -FallSpeed);
			
			
			
		}
	falling:
		if (velocity.y < 0){									//play fall animation
			falling = true;
			jumping = false;
			if (!anim.IsPlaying(a_fall) && !sprinting && !fallingSlowly && !agape && !exhaling && !mouthFull && !charging){
				anim.Play(a_fall);
			}
		}
		
		if (exhaling && !anim.IsPlaying(a_exhale)){
			exhaling = false;
			fallingSlowly = false;
			
		}
	}
	
					//functions that change my motor!
	
	public void ChangeToMondo (){
		currentMotor = mondo;
		Destroy(dummy);
		dummy = Instantiate(Resources.Load("mondoDummy"), t.position - Vector3.forward, t.rotation) as GameObject;
		velocity = Vector2.zero;
		anim = dummy.GetComponent<tk2dSpriteAnimator>();
		anim.Play(grounded? a_idle : a_fall);
		dummy.transform.parent = t;
		sprite = dummy.GetComponent<tk2dSprite>();
		bc.size = new Vector3(sprite.CurrentSprite.colliderVertices[1].x * 2, sprite.CurrentSprite.colliderVertices[1].y * 2, 10);
		sprinting = false;
		
		RunDown = null;
		Fall = null;
		Gravity = null;
		JumpDown = MondoJumpDown;
		DownDown = MondoDownDown;
		OnLand -= PinkyOnLand;
		OnLand += MondoOnLand;
		OnLand -= BoogerOnLand;
	}
	
	public void ChangeToPinky (){
		currentMotor = pinky;
		Destroy(dummy);
		dummy = Instantiate(Resources.Load("pinkyDummy"), t.position - Vector3.forward, t.rotation) as GameObject;
		velocity = Vector2.zero;
		Debug.Log("Should be loading pinky now");
		anim = dummy.GetComponent<tk2dSpriteAnimator>();
		anim.Play(grounded? a_idle : a_fall);
		dummy.transform.parent = t;
		sprite = dummy.GetComponent<tk2dSprite>();
		bc.size = new Vector3(sprite.CurrentSprite.colliderVertices[1].x * 2, sprite.CurrentSprite.colliderVertices[1].y * 2, 10);
		exhaling = false;
		agape = false;
		inMouth = null;
		mouthFull = false;
		Busy = false;
		sprinting = false;
		//change delegates and shit
		Fall = null;
		Gravity = null;  //gonna have to replace this stuff when the time comes
		RunDown = PinkyRunDown;
		RunUp = PinkyRunUp;
		JumpDown = PinkyJumpDown;
		DownDown = PinkyDownDown;
		DownUp = PinkyDownUp;
		OnLand += PinkyOnLand;
		OnLand -= MondoOnLand;
		OnLand -= BoogerOnLand;
	}
	
	public void ChangeToBoogerBoy () {
		currentMotor = boogerBoy;
		Destroy(dummy);
		dummy = Instantiate(Resources.Load("boogerDummy"), t.position - Vector3.forward, t.rotation) as GameObject;
		velocity = Vector2.zero;
		Debug.Log("Should be loading booger boy now");
		anim = dummy.GetComponent<tk2dSpriteAnimator>();
		anim.Play(grounded? a_idle : a_fall);
		dummy.transform.parent = t;
		sprite = dummy.GetComponent<tk2dSprite>();
		bc.size = new Vector3(sprite.CurrentSprite.colliderVertices[1].x * 2, sprite.CurrentSprite.colliderVertices[1].y * 2, 10);
		/*
		exhaling = false;
		agape = false;
		inMouth = null;
		mouthFull = false;
		Busy = false;
		sprinting = false;
		*/
		//change delegates and shit
		
		OnLand -= PinkyOnLand;
		OnLand -= MondoOnLand;
		OnLand += BoogerOnLand;
		
						//call this on fall
		Fall = delegate(){
			anim.Play (a_fall);
		};
						//apply gravity
		Gravity = delegate(){
			velocity = new Vector2(velocity.x, Mathf.Max (velocity.y - boogerBoy.gravity, -boogerBoy.fallSpeedMax));
		};
						//run button pressed: Shoot/charge
		Run = delegate(){
			Debug.Log ("Shoot!");
		};
						//run button up: Release charge
		RunUp = delegate(){
			Debug.Log ("Release if I've been charging!");
		};
		
						//jump button pressed: jump
		JumpDown = delegate(){
			if (!grounded) return;
			velocity = new Vector2(velocity.x, boogerBoy.airSpeedInit);
			anim.Play (a_jump);
		};
						//jump button released: fall
		JumpUp = delegate() {
			velocity = new Vector2(velocity.x, 0);
		};
		
						//pressing a direction (left-right): move
		Direction = delegate(float amount) {
			if (directionTimer == 0 && grounded){
				anim.Play (a_walk);
			}
			directionTimer = Mathf.Min (directionTimer + 1, DirectionMax);
			velocity = new Vector2(CurMaxSpeed * (directionTimer / DirectionMax) * (amount < 0? -1 : 1), velocity.y);
		};
		
						//just pressed a direction
		DirectionDown = delegate() {
			CancelInvoke("BoogerStop");
		};
		
						//direction release
		DirectionUp = delegate(){
			Invoke ("BoogerStop", Time.deltaTime * 4);
		};
		
						//double tap : Dash!
		DoubleTap = delegate() {
			Debug.Log ("Dash!");
			dashing = true;
			anim.Play (a_dash);
			
		};
		
						//down doesn't do anything for Megaman
		DownDown = delegate(){
			
		};
		
		DownUp = delegate(){
			
		};
	}
	private int directionTimer = 0;
	private int DirectionMax {
		get { return dashing? 10 : 5; }
	}
	private bool dashing = false;
	
	
	public void BoogerStop () {
		directionTimer = 0;
		velocity = new Vector2(0, velocity.y);
		if (grounded)
			anim.Play (a_idle);
		if (dashing){
			anim.Play (a_endDash);
		}
	}

	
	public void BoogerOnLand () {
		dashing = false;
		if (directionTimer > 0){
			anim.Play (anim.GetClipByName(a_walk), anim.ClipFps * DirectionMax, anim.ClipFps);
		}
		else{
			anim.Play (a_land);
		}
	}
											//<^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^>
											//<:::::::::::::::::PINKY EVENT FUNCTIONS::::::::::::::::::::>
											//<vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv>
	public void PinkyRunDown() {
		if (busy) return;
		//exhale my air
		if (fallingSlowly){
			exhaling = true;
			anim.Play(a_exhale);
			return;
		}
		//start charging
		else if (isDoc){
			if (!charging){
				charging = true;
				anim.Play (a_curl);
				anim.AnimationCompleted = StartCharging;
			}
			else{
				Debug.Log("Stop charging");
				charging = false;
			}
			return;
		}
		
		//spit it out
		if (mouthFull) {
			exhaling = true;
			agape = false;
			mouthFull = false;
			anim.Play (a_exhale);
			inMouth.transform.position = FacingRight? rightDetector.transform.position + Vector3.up * 8 : leftDetector.transform.position + Vector3.up * 8;
			inMouth.SendMessage("Kicked");
			inMouth = null;
			Busy = true;
			return;
		}
		//just open my mouth and get ready to swallow
		Busy = true;
		Debug.Log("Rundown!");
		agape = true;
		anim.Play(a_agape);
		fallingSlowly = false;
		jumping = false;
	}
	
	public void PinkyRunUp() {
		
		if (busy){
			willCloseMouth = true;
		}
		else {
			agape = false;
		}
	}
	
	public void PinkyJumpDown(){
		if (crouching && !sliding){
			Sliding = true;
			return;
		}
		else if (sliding){
			return;
		}
		
		if (charging) return;
		
		if (!agape && grounded){		//jump initiated!
			velocity = new Vector2(velocity.x, pinky.airSpeedInit);
			jumping = true;
			grounded = false;
			if (!anim.IsPlaying(PinkyJump)){
				anim.Play(PinkyJump);
			}
			crouching = false;
		}
		
		else if (!grounded && !agape && !mouthFull){
			fallingSlowly = true;
			jumping = true;
			sprinting = false;
			velocity = new Vector2(velocity.x, pinky.jExtraHeight);
			anim.Play(a_float);
		}
	}
	
	public void PinkyOnLand (){
		if (agape){
			sprinting = false;
			velocity = Vector2.zero;
			return;
		}
		if (sprinting && !controller.getL && !controller.getR){
			sprinting = false;
			
		}
		else if (sprinting && !charging){
			anim.Play(PinkyRun);
		}
		else{
			if (fallingSlowly){
				anim.Play(a_exhale);
				exhaling = true;
			}
		}
		
		if (controller.getD){
			PinkyDownDown ();
		}
	}
	
	public void DoneSwallowing (tk2dSpriteAnimator anim, tk2dSpriteAnimationClip clip){
		swallowing = false;
		Doc docScript = inMouth.GetComponent<Doc>();
		if (docScript != null){
			Debug.Log ("I am a walrus");
			IsDoc = true;
		}
		inMouth = null;
		mouthFull = false;
	}
	
	public void PinkyDownDown (){  //HACK. LIKE, MEGA HACK
		if (!grounded) return;
		
		//Swallow what's in my mouth
		if (inMouth != null){
			swallowing = true;
			anim.Play (a_swallow);
			anim.AnimationCompleted = DoneSwallowing;
			return;
		}
		
		//crouch
		anim.Play (a_crouch);
		FitDetectors ();
		grounded = true;
		falling = false;
		crouching = true;
	}
	
	public void PinkyDownUp(){
		if (sliding || !grounded || swallowing) return;
		//float groundy = t.position.y - bc.bounds.size.y/2;		//such a hack I can't even
		anim.Play (a_idle);
		crouching = false;
		FitDetectors ();
		//t.position = new Vector3(t.position.x, groundy + bc.bounds.size.y/2, t.position.z);
		
	}
	
	public void PinkyBigJumpAnim (tk2dSpriteAnimator anim, tk2dSpriteAnimationClip clip, int id) {
		anim.AnimationEventTriggered = null;
		anim.Play(a_jumpBig);
	}
	
	//small methods for Pinky's roll-up-and-charge ability
	public void StartCharging (tk2dSpriteAnimator anim, tk2dSpriteAnimationClip clip){
		velocity = new Vector2(pinky.sSpeed * FacingRightMod, velocity.y);
		anim.Play (a_charge);
		if ((FacingRight && !canMoveRight) || (!FacingRight && !canMoveLeft)){
			EndCharge ();
		}
	}
	
	public void EndCharge (){
		anim.Play (a_uncurl);
		velocity = Vector2.zero;
		anim.AnimationCompleted = DoneUncurl;
	}
	
	public void DoneUncurl (tk2dSpriteAnimator anim, tk2dSpriteAnimationClip clip){
		charging = false;
	}
	
											//<^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^>
											//<:::::::::::::::::MONDO EVENT FUNCTIONS::::::::::::::::::::>
											//<vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv>
	public void MondoOnLand (){
		
	}
	
	
	public void MondoJumpDown(){	
		if (!grounded) return;
		
		jumping = true;
		grounded = false;
		
		velocity = new Vector2(velocity.x, mondo.airSpeedInit);
		float calculator = Mathf.Abs(velocity.x) - mondo.runJumpIncrement;		//add extra height based on jump. 
		while(calculator > 0){
			velocity += new Vector2(0, mondo.airSpeedExtra);
			calculator -= mondo.runJumpIncrement;
		}
		if (!anim.IsPlaying(a_jump) && !anim.IsPlaying(a_sJump) && !mouthFull){
			anim.Play(sprinting? a_sJump : a_jump);
			Debug.Log("Playing jump!");
		}
	}
	
	public void MondoDownDown () {
		Debug.Log ("Mondo Crouch!");
	}
	
											//<^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^>
											//<::::::::::::::::BOOGER EVENT FUNCTIONS::::::::::::::::::::>
											//<vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv>
	//booger boy variables

}
