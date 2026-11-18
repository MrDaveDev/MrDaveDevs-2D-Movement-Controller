extends CharacterBody2D

@export_group("Movement")
@export var accelTime: float = 0.5
@export var airAccelTime: float = 0.2
@export var maxSpeed: float = 250
@export var decelTime: float = 0.2
@export var airDecelTime: float = 0.5
@export var turnTime: float = 0.1

@export_group("Jump")
@export var jumpHeight: float = 3
@export var jumpTime: float = 0.25
@export var hangTime: float = 0.1
@export var variableJumpHeight: bool = true

@export_group("Gravity")
@export var maxFallSpeed: float = 1000
@export var defGravity: float = 9.81
@export var fallGravity: float = 50
@export var coyoteTiming: float = 0.15

# Private Jump Variables
var jumpGravity: float
var currentJumpTimer: float
var currentHangTimer: float
var currentCoyoteTimer: float
var canJump: bool = true
var hasJumped: bool = false
var ascending: bool = false
var hanging: bool = false
var spaceDown: bool = false

# Runs when the object enters the scene
func _ready() -> void:
	canJump = is_on_floor()

# Runs once every physics frame
func _physics_process(delta: float) -> void:
	_handleJump(delta)
	_handleMove(delta)
	_restart()
	move_and_slide()
	_handleCeilingCollision()

# Handles jump states
func _handleJump(delta: float) -> void:
	var grounded: bool = is_on_floor()
	
	if grounded:
		currentCoyoteTimer = 0
		
		if !ascending && !hanging:
			canJump = true
			hasJumped = false
	else:
		currentCoyoteTimer += delta
		canJump = !hasJumped && currentCoyoteTimer < coyoteTiming
	
	if Input.is_action_just_pressed("jump"): _calculateJump()
	elif Input.is_action_just_released("jump"):
			spaceDown = false;

			if ascending && variableJumpHeight:
				velocity.y = 0

				ascending = false;
				hanging = false;
	
	if ascending:
		currentJumpTimer += delta
		velocity.y += jumpGravity * delta
		
		if currentJumpTimer >= jumpTime:
			hanging = spaceDown && hangTime > 0
			currentHangTimer = 0
			ascending = false
	elif hanging:
		currentHangTimer += delta
		velocity.y += fallGravity * 0.8 * delta
		
		if currentHangTimer > hangTime || !spaceDown:
			hanging = false
			ascending = false
	else:
		if !grounded && !hanging:
			var gravity: Vector2 = Vector2(0, 1) * defGravity * fallGravity * delta
			velocity.y = minf(velocity.y + gravity.y, maxFallSpeed)
		else:
			velocity.y = 0

# Handles and applies movement states and physics
func _handleMove(delta: float) -> void:
	var moveInput: float = _getMoveValue()
	var grounded: bool = is_on_floor()
	
	if abs(moveInput) > 0:
		var weight: float
		if abs(velocity.x) > 0.01 && sign(velocity.x) != sign(moveInput): 
			weight = 1.0 - exp(-5.0 * delta / max(turnTime, 0.001))
		elif grounded:
			weight = 1.0 - exp(-5.0 * delta / max(accelTime, 0.001))
		else: 
			weight = 1.0 - exp(-5.0 * delta / max(airAccelTime, 0.001))
		
		velocity.x = lerp(velocity.x, maxSpeed * moveInput, weight)
	else:
		var weight: float
		if grounded: weight = 1.0 - exp(-5.0 * delta / max(decelTime, 0.001))
		else: weight = 1.0 - exp(-5.0 * delta / max(airDecelTime, 0.001))
	
		velocity.x = lerp(velocity.x, 0.0, weight)

# Prevents hangtime when colliding with a ceiling
func _handleCeilingCollision() -> void:
	if is_on_ceiling() && ascending:
		ascending = false
		hanging = false
		velocity.y = 0

# Handles restarting the player
func _restart() -> void:
	if Input.is_action_just_pressed("restart"):
		position = Vector2(580, 323)
		velocity = Vector2(0, 0)
		
		ascending = false
		hanging = false
		spaceDown = false
		hasJumped = false
		canJump = false
		currentJumpTimer = 0
		currentHangTimer = 0
		currentCoyoteTimer = 0

# Returns -1 for left, +1 for right, 0 for neither
func _getMoveValue() -> float:
	return Input.get_axis("move_left", "move_right")

# Calculates and applies the jump physics
func _calculateJump() -> void:
	if canJump && !hasJumped:
		hasJumped = true
		canJump = false
		
		var sprite: Sprite2D = $Sprite2D
		var spriteHeight: float = sprite.texture.get_height() / float(sprite.vframes) * sprite.global_scale.y
		
		jumpGravity = (2 * jumpHeight * spriteHeight) / (max(jumpTime, 0.001) * max(jumpTime, 0.001))
		var jumpVel: float = -jumpGravity * jumpTime;

		velocity.y = jumpVel

		spaceDown = true;
		ascending = true;
		hanging = false;
		currentJumpTimer = 0;
		currentHangTimer = 0;
