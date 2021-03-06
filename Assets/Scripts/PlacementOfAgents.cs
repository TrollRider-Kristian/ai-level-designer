﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

// Use three Bezier Curves, https://en.wikipedia.org/wiki/B%C3%A9zier_curve 
// to place enemy agents in the game.  Randomly determine control points at locations
// START -> (0, 0), (3, random(-5, 5)), (6, random(-5, 5)), (9, random(-2.5, 2.5)) <- GOAL for each curve.
// 2. Sample random points wrt time between 0 and 1 and place the agents along the first curve.
// 3. If the random value t is less than 0.4 or greater than 0.6, add an agent along the second curve at 1 - t and a third on the third curve at random.
//  Else add two agents on the second and third curves, one at t + t/2 and one at t - t/2.

public class PlacementOfAgents : MonoBehaviour
{
    public InputField input_num_distinct_sprites;
  
    public RawImage sprite_to_add;

    public Text how_many, which_sprite, which_interaction;

    public Button nextButton;
    public Button browse_for_sprites;
    public Button next_sprite;

    public Button radio_collect;
    public Button radio_avoid;
    public Button radio_defeat;

    bool collect_selected;
    bool avoid_selected;
    bool defeat_selected;

    public GameObject[] moving_agents;
    List<GameObject> agents_in_game;
    public GameObject[] landscapes;
    List<GameObject> landscapes_in_game;
    public GameObject hero, flag, you_win;

    //control points first Bezier Curve:
    float x1, y1, x2, y2, x3, y3, x4, y4;
    //control points second Bezier Curve:
    float a1, b1, a2, b2, a3, b3, a4, b4;
    //control points third Bezier Curve:
    float c1, d1, c2, d2, c3, d3, c4, d4;

    string path_to_sprite;
    int sprite_index;

    bool game_is_playing, game_over;

    bool flag_placed, hero_instantiated; //because the flag and hero are instantiated, we want to set them active instead of making new ones.

    // Use this for initialization
    void Start()
    {
        agents_in_game = new List<GameObject>();
        landscapes_in_game = new List<GameObject>();

        path_to_sprite = "";
        sprite_index = 0;
        how_many.gameObject.SetActive(true);
        deactivate_sprite_loader();
        game_is_playing = false;
        you_win.gameObject.SetActive(false);
        game_over = false;

        flag_placed = false;
        hero_instantiated = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (game_is_playing && !flag.activeSelf && !game_over) //flag is not active, it's instantiated...
        {
            deactivate_old_game();
            you_win.gameObject.SetActive(true);
            game_over = true;
        }

        if (game_over && Input.GetKeyDown(KeyCode.Y))
        {
            game_is_playing = false;
            you_win.gameObject.SetActive(false);
            game_over = false;
            loadEntireGame(Random.Range(3, 5)); 
            hero.gameObject.SetActive(true); 
            flag.gameObject.SetActive(true);
        }

        if (game_over && Input.GetKeyDown(KeyCode.N))
        {
            game_is_playing = false;
            you_win.gameObject.SetActive(false);
            game_over = false;
            input_num_distinct_sprites.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(true);
            how_many.gameObject.SetActive(true);
        }
    }

    void deactivate_old_game()
    {
        agents_in_game.ForEach(ag => Destroy(ag));
        agents_in_game.ForEach(ag => agents_in_game.Remove(ag));

        landscapes_in_game.ForEach(la => Destroy(la));
        landscapes_in_game.ForEach(la => landscapes_in_game.Remove(la));
    }

    void activate_sprite_loader()
    {
        deactivate_how_many_prompter();
        activate_questions();
        activate_sprite_buttons();
        activate_radios_helper();
        sprite_index = 0;
    }

    void activate_sprite_buttons()
    {
        sprite_to_add.gameObject.SetActive(true);
        browse_for_sprites.gameObject.SetActive(true);
        next_sprite.gameObject.SetActive(true);
    }

    void activate_questions()
    {
        which_sprite.gameObject.SetActive(true);
        which_interaction.gameObject.SetActive(true);
    }

    void deactivate_how_many_prompter()
    {
        input_num_distinct_sprites.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        how_many.gameObject.SetActive(false);
    }

    void activate_radios_helper()
    {
        radio_collect.gameObject.SetActive(true);
        radio_avoid.gameObject.SetActive(true);
        radio_defeat.gameObject.SetActive(true);
        select_defeat();
    }

    void deactivate_sprite_loader()
    {
        sprite_to_add.gameObject.SetActive(false);
        browse_for_sprites.gameObject.SetActive(false);
        next_sprite.gameObject.SetActive(false);
        which_sprite.gameObject.SetActive(false);
        which_interaction.gameObject.SetActive(false);
        deactivate_radios_helper();
    }

    void deactivate_radios_helper()
    {
        radio_collect.gameObject.SetActive(false);
        radio_avoid.gameObject.SetActive(false);
        radio_defeat.gameObject.SetActive(false);
    }

    public void nextClicked()
    {
        if (input_num_distinct_sprites.text.Length > 0)
        {
            try
            {
                int num_distinct_sprites = int.Parse(input_num_distinct_sprites.text);
                moving_agents = new GameObject[num_distinct_sprites];
                activate_sprite_loader();
            }
            catch (System.FormatException)
            {
                //handle bad formatting here
            }
            catch (System.OverflowException)
            {
                //overflow input here
            }
        }
    }

    // Using this video to specify input sprites to the game: https://www.youtube.com/watch?v=Vh_XkNwThg4
    public void OpenExplorer()
    {
        path_to_sprite = EditorUtility.OpenFilePanel("Overwrite with png", "", "png");
        if (path_to_sprite.Length > 0)
        {
            WWW www = new WWW("file:///" + path_to_sprite);
            sprite_to_add.texture = www.texture;
        }

    }

    public void nextSprite()
    {
        GameObject agent_to_add = new GameObject("agent #" + sprite_index);

        //https://forum.unity.com/threads/converting-texture-to-texture2d.25991/
        Sprite sprite_to_handle = wrap_image_into_sprite(sprite_to_add.texture as Texture2D);
        //https://forum.unity.com/threads/how-to-programmatically-add-sprite-to-spriterenderer.257990/
        SpriteRenderer rend_agent = agent_to_add.AddComponent(typeof(SpriteRenderer)) as SpriteRenderer;
        rend_agent.sprite = sprite_to_handle;
        //move the transform far far away so it doesn't display on our screen until we want it to, since it's not a prefab
        rend_agent.transform.position = new Vector3(-9000.0f, -9000.0f, -9000.0f);

        if (collect_selected)
        {
            agent_to_add.tag = "Collect";
        }

        if (avoid_selected)
        {
            agent_to_add.tag = "Avoid";
        }

        if (!defeat_selected)
        {
            BoxCollider2D col = agent_to_add.AddComponent(typeof(BoxCollider2D)) as BoxCollider2D;
            col.isTrigger = true;
        }

        if (defeat_selected)
        {
            agent_to_add.tag = "Defeat";
            //https://answers.unity.com/questions/774087/how-to-create-change-edge-collider-from-code.html 
            EdgeCollider2D col = agent_to_add.AddComponent(typeof(EdgeCollider2D)) as EdgeCollider2D;
            List<Vector2> col_pts = new List<Vector2>();
            col_pts.Add(new Vector2(-1.0f, -0.5f));
            col_pts.Add(new Vector2(-1.0f, 1.0f));
            col_pts.Add(new Vector2(1.0f, 1.0f)); 
            col_pts.Add(new Vector2(1.0f, -0.5f));
            col.points = col_pts.ToArray();
            col.isTrigger = true;
        }

        //https://answers.unity.com/questions/1136397/how-to-add-a-script-to-a-gameobject-during-runtime.html
        InteractionChecker script = agent_to_add.AddComponent<InteractionChecker>();
        Debug.Log("Now adding sprite at index: " + sprite_index);
        moving_agents[sprite_index] = agent_to_add;
        sprite_index++;
        clear_sprite_helper();

        if (sprite_index >= moving_agents.Length)
        {
            deactivate_sprite_loader();
            loadEntireGame(Random.Range(3, 5));
        } 
    }

    void clear_sprite_helper()
    {
        Texture blank = new Texture2D(sprite_to_add.texture.width, sprite_to_add.texture.height) as Texture;
        sprite_to_add.texture = blank;
    }

    Sprite wrap_image_into_sprite(Texture2D tex)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    public void select_collect()
    {
        collect_selected = true;
        avoid_selected = false;
        defeat_selected = false;

        radio_collect.image.color = Color.red;
        radio_avoid.image.color = Color.white;
        radio_defeat.image.color = Color.white;
    }

    public void select_avoid()
    {
        collect_selected = false;
        avoid_selected = true;
        defeat_selected = false;

        radio_collect.image.color = Color.white;
        radio_avoid.image.color = Color.red;
        radio_defeat.image.color = Color.white;
    }

    public void select_defeat()
    {
        collect_selected = false;
        avoid_selected = false;
        defeat_selected = true;

        radio_collect.image.color = Color.white;
        radio_avoid.image.color = Color.white;
        radio_defeat.image.color = Color.red;
    }

    void loadEntireGame(int agentsPerCurve)
    {
        define_first_Bezier();
        define_second_Bezier();
        define_third_Bezier();
        load_gameLevel(agentsPerCurve);
        game_is_playing = true;
    }

    void define_first_Bezier()
    {
        x1 = 0.0f;
        y1 = 0.0f;
        x2 = 15.0f;
        y2 = Random.Range(-15.0f, 15.0f);
        x3 = 30.0f;
        y3 = Random.Range(-15.0f, 15.0f);
        x4 = 45.0f;
        y4 = Random.Range(-7.2f, 7.2f);
    }

    void define_second_Bezier()
    {
        a1 = 0.0f;
        b1 = 0.0f;
        a2 = 15.0f;
        b2 = Random.Range(-15.0f, 15.0f);
        a3 = 30.0f;
        b3 = Random.Range(-15.0f, 15.0f);
        a4 = 45.0f;
        b4 = Random.Range(-7.2f, 7.2f);
    }

    void define_third_Bezier()
    {
        c1 = 0.0f;
        d1 = 0.0f;
        c2 = 15.0f;
        d2 = Random.Range(-15.0f, 15.0f);
        c3 = 30.0f;
        d3 = Random.Range(-15.0f, 15.0f);
        c4 = 45.0f;
        d4 = Random.Range(-7.2f, 7.2f);
    }

    void load_gameLevel(int agentsPerCurve)
    {
        place_hero();
        placeAgents(agentsPerCurve);
        place_goalpost();
    }

    void place_hero()
    {
        if (!hero_instantiated)
        {
            hero = Instantiate(hero, transform.position, transform.rotation) as GameObject;
            hero_instantiated = true;
        }

        else
        {
            hero.gameObject.SetActive(true);
        }

        hero.transform.position = new Vector3(0, 0, 0);
        land_placement_helper(hero);
    }

    void placeAgents(int agentsPerCurve)
    {
        //sample numbers between 0 and 1 to place agents on the first curve
        //determine second and third curve such that agents are "well distributed"
        for (int i = 0; i < agentsPerCurve; i++)
        {
            float t = Random.Range(0.1f, 1.0f);
            float u, v;

            if (t < 0.4 || t > 0.6)
            {
                u = 1.0f - t;
                v = Random.Range(0.3f, 0.7f);
            }
            else
            {
                int coin_flip = Random.Range(0, 2);
                if (coin_flip == 0)
                {
                    u = t + (t / 2);
                    v = t - (t / 2);
                }
                else
                {
                    u = t - (t / 2);
                    v = t + (t / 2);
                }
            }

            float xLoc = BezierHelper(x1, x2, x3, x4, t);
            float yLoc = BezierHelper(y1, y2, y3, y4, t);
            float aLoc = BezierHelper(a1, a2, a3, a4, u);
            float bLoc = BezierHelper(b1, b2, b3, b4, u);
            float cLoc = BezierHelper(c1, c2, c3, c4, v);
            float dLoc = BezierHelper(d1, d2, d3, d4, v);

            placeAgentsHelper(xLoc, yLoc);
            placeAgentsHelper(aLoc, bLoc);
            placeAgentsHelper(cLoc, dLoc);
        }
    }

    float BezierHelper(float x1, float x2, float x3, float x4, float t)
    {
        return Mathf.Pow(1 - t, 3) * x1 + 3 * Mathf.Pow(1 - t, 2) * x2 * t + (1 - t) * x3 * Mathf.Pow(t, 2) + x4 * Mathf.Pow(t, 3);
    }

    //VERY useful for the three below functions: https://answers.unity.com/questions/686840/how-to-instantiate-sprite-prefab-c.html
    void placeAgentsHelper(float xLoc, float yLoc)
    {
        GameObject monster = Instantiate(moving_agents[Random.Range(0, moving_agents.Length)], transform.position, transform.rotation) as GameObject;
        monster.transform.position = new Vector3(xLoc, yLoc, 0);
        land_placement_helper(monster);

        agents_in_game.Add(monster);
    }

    void place_goalpost()
    {
        if (!flag_placed)
        {
            flag = Instantiate(flag, transform.position, transform.rotation) as GameObject;
            flag_placed = true;
        }

        else
        {
            flag.gameObject.SetActive(true);
        }

        flag.transform.position = new Vector3(x4, y4, 0);
        land_placement_helper(flag);
    }

    void land_placement_helper(GameObject agent)
    {
        GameObject landscape = Instantiate(landscapes[0], transform.position, transform.rotation) as GameObject;
        SpriteRenderer land_under_me = agent.GetComponent<SpriteRenderer>();
        SpriteRenderer widthscape = landscape.GetComponent<SpriteRenderer>();
        landscape.transform.position = agent.transform.position - new Vector3(0, land_under_me.bounds.extents.y + 2 * widthscape.bounds.extents.y, 0);

        landscapes_in_game.Add(landscape);
    }
}
