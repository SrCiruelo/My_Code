using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class My_Rigidbody_2D : MonoBehaviour
{

    // Use this for initialization
    private Rigidbody2D my_rigid;
    [SerializeField]
    private float gravity_mod = 1f;


    private float horizontal;
    private float vertical;

    [SerializeField]
    private float speed;

    private RaycastHit2D[] hit_buffer = new RaycastHit2D[32];

    [SerializeField]
    private float min_move_distance = 0.001f;
    [SerializeField]
    [Tooltip("This variable is used to cast the collider further the direction we want to move so we make sure it won't enter into another collision with 0.01 IS FINE")]
    private float distance_shell = 0.01f;
    [Tooltip("This number tells difference between being grounded in a slightly tilted ramp and highly tilted ramp. The higher the number is the less tilted the ramp will be . GOTTA BE MORE LESS THAN ONE ALWAYS")]
    [SerializeField]
    private float min_ground_normal_Y = 0.65f;
    [SerializeField]
    [Tooltip("If the distance that is going to move is higher than this number it will reduce the velocity but won't cancel it")]
    private float distance_reduce = 2f;

    [HideInInspector]
    public Vector2 velocity;
    [HideInInspector]
    public Vector2 movement_velocity;
    [HideInInspector]
    public Vector2 external_forces_velocity;
    [HideInInspector]
    public Vector2 gravity_force_velocity;

    private Vector2 normal;

    private ContactFilter2D my_contact_filter;
    private PhysicsMaterial2D ground_material;
    private PhysicsMaterial2D my_material;

    public bool grounded { get; private set; }





    //THIS WILL NOT BE IN FINAL CODE
    private bool explosion_request = false;
    [SerializeField]
    private Vector2 my_vel;
    [SerializeField]
    private float jump_speed;




    void Start()
    {
        my_rigid = (Rigidbody2D)GetComponent(typeof(Rigidbody2D));
        my_material = my_rigid.sharedMaterial;
        //we tell the filter not to check collisions with triggers
        my_contact_filter.useTriggers = false;
        //we tell the filter to use the physics project settings layermask collisions
        my_contact_filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
    }

    // Update is called once per frame
    void Update()
    {
        vertical = Input.GetAxisRaw("Vertical");
        horizontal = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.Space)) explosion_request = true;
    }
    private void FixedUpdate()
    {
        grounded = false;
        ground_material = null;
        add_gravity();
        add_movement_force(new Vector2(horizontal * speed, 0));


        //NOT IN FINAL CODE
        if (explosion_request) { add_movement_impulse(Vector2.up * jump_speed); explosion_request = false; }
        //add_explosion_force(my_vel);

        //NOT IN FINAL CODE
        //NEED A REVISION BUG IN MOVEMENT_VELOCITY
        //if (horizontal == 0)
        //{
        //    Vector2 new_move = new Vector2(-movement_velocity.x, 0);
        //    add_movement_force(new_move / Time.fixedDeltaTime);
        //}

        //velocity * Time.fixedDeltaTime converts velocity the vector to just direction vector in space
        HashSet<Vector2> my_set = new HashSet<Vector2>();
        Move(velocity * Time.fixedDeltaTime, ref my_set);

        //add_friction();

        //NOT IN CODe
        //print("  movement " + movement_velocity.x);
        //print("external " + external_forces_velocity);
        //print(" gravity " + gravity_force_velocity);
        //print(" normal " + normal);
    }


    #region Collisions and Movement
    private void Move(Vector2 move, ref HashSet<Vector2> vectors_passed)
    {
        float distance = move.magnitude;
        float distance_to_collision = sizeof(float);
        int count = 0;
        if (distance > min_move_distance)
        {
            //Here we detect future possible overlaps 
            count = my_rigid.Cast(move, my_contact_filter, hit_buffer, distance_shell + distance);
            //Debug.DrawRay(transform.position, move.normalized * distance, Color.cyan);
            Vector2[] all_normals = new Vector2[count];
            // REVISAR ESTE CÓDIGO OPTIMIZAR
            for (int i = 0; i < count; ++i)
            {
                Vector2 current_normal = hit_buffer[i].normal;
                all_normals[i] = current_normal;

                if (current_normal.y > min_ground_normal_Y)
                {
                    grounded = true;
                }
                float modidified_distance = hit_buffer[i].distance + distance_shell;
                if (modidified_distance < distance) distance = modidified_distance - distance_shell;
                if (hit_buffer[i].distance < distance_to_collision) { distance_to_collision = hit_buffer[i].distance; }

            }
            if (grounded)
            {
                //Here we will change the gravity direction

            }


            //WHEN COLLIDING WITH 2 OBJECTS (OR MORE) AT THE SAME TIME IT FAILS CAUSE WE NEED TO APPLY DIFFERENT NORMALS
            //normal += normal* bounciness;
            //Debug.DrawRay(transform.position, velocity);
            int cont = 0;
            //print(velocity + "   " + count


            Vector2? loc_velX = null;
            foreach (Vector2 i in all_normals)
            {
                Debug.DrawRay(hit_buffer[cont].point, i, Color.red);

                float bounciness = 0;
                ground_material = hit_buffer[cont].collider.sharedMaterial;
                if (ground_material != null)
                {
                    bounciness = ground_material.bounciness;
                }
                //float distance_reduce = (distance > this.distance_reduce)? 1 / (Mathf.Log10(distance - distance_shell + 1) + 1) : 1;
                Vector2 local_normal = hit_buffer[cont].normal.normalized;
                normal = calculate_normal(local_normal, -velocity);
                loc_velX = calculate_normal(new Vector2(-local_normal.y, local_normal.x), velocity);

                #region local_forces
                do_move(local_normal);
                #endregion

                velocity += normal;
                ++cont;
            }

            //print(distance + "   " + Mathf.Round(1 / (Mathf.Log10(distance - distance_shell + 1) + 1) * 10f) / 10f);

            //PREGUNTAR COMO MEJORAR ESTO Y FORMA DE PLANTEARLO
            if (loc_velX != null)
            {
                if (vectors_passed.Contains(loc_velX.Value.normalized))
                {
                    return;
                }
                if (loc_velX.Value != Vector2.zero)
                {
                    vectors_passed.Add(loc_velX.Value.normalized);
                    Debug.DrawRay(transform.position, new Vector3(loc_velX.Value.x, loc_velX.Value.y, 0) * Time.fixedDeltaTime, Color.cyan);
                    Debug.DrawRay(transform.position, -velocity * Time.fixedDeltaTime, Color.yellow);
                    Move(loc_velX.Value * Time.fixedDeltaTime, ref vectors_passed);

                }
                else
                {
                    do_move(velocity);
                }
            }
            else
            {
                do_move(velocity);
            }
        }
    }
    #endregion 

    private void do_move(Vector2 velocity)
    {
        Vector2 move = velocity * Time.fixedDeltaTime;
        my_rigid.MovePosition(my_rigid.position + move);
    }
    private void local_forces(Vector2 local_normal)
    {
        Vector2 gravity_normal = -calculate_normal(gravity_force_velocity, -local_normal);
        gravity_force_velocity += (gravity_normal.normalized == gravity_force_velocity.normalized) ? Vector2.zero : gravity_normal;

        Vector2 move_normal = -calculate_normal(movement_velocity, -local_normal);
        movement_velocity += (move_normal.normalized == movement_velocity.normalized) ? Vector2.zero : move_normal;

        Vector2 external_normal = -calculate_normal(external_forces_velocity, -local_normal);
        external_forces_velocity += (external_normal.normalized == external_forces_velocity.normalized) ? Vector2.zero : external_normal;
    }
    private void add_gravity()
    {
        //Convert acceleration to velocity
        Vector2 gravity = Physics2D.gravity * Time.fixedDeltaTime * gravity_mod;
        velocity += gravity;
        gravity_force_velocity += gravity;
    }
    private void add_friction()
    {
        // WE NEED TO DIVIDE IT IN CONTROL GRAVITY AND OTHER FORCES
        if (ground_material != null)
        {
            velocity -= velocity * ground_material.friction;
            gravity_force_velocity -= gravity_force_velocity * ground_material.friction;
            movement_velocity -= movement_velocity * ground_material.friction;
            external_forces_velocity -= external_forces_velocity * ground_material.friction;
        }
        else if (grounded)
        {
            velocity += -velocity * my_material.friction;
            gravity_force_velocity -= gravity_force_velocity * my_material.friction;
            movement_velocity -= movement_velocity * my_material.friction;
            external_forces_velocity -= external_forces_velocity * my_material.friction;
        }
    }
    private Vector2 calculate_normal(Vector2 direction, Vector2 total_normal)
    {
        Vector2 my_normal = direction.normalized * Vector2.Dot(direction, total_normal);
        return my_normal;
    }
    public void add_movement_force(Vector2 my_force)
    {
        Vector2 moving_acc = my_force * Time.fixedDeltaTime;
        movement_velocity += moving_acc;
        velocity += moving_acc;
    }
    public void add_movement_impulse(Vector2 my_force)
    {
        Vector2 moving_acc = my_force;
        movement_velocity += moving_acc;
        velocity += moving_acc;
    }
    public void add_explosion_force(Vector2 my_force)
    {
        Vector2 moving_acc = my_force * Time.fixedDeltaTime;
        external_forces_velocity += moving_acc;
        velocity += moving_acc;
    }
    public void add_explosion_impulse(Vector2 my_force)
    {

    }
}