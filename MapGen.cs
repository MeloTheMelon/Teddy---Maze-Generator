using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapGen : MonoBehaviour {

    [System.Serializable]
    public class Box {                               //Das Box Objekt beinhalted die Informationen zu den zugehörigen Wänden

        public bool visited;                        //Gebraucht für DFS
        public GameObject north;   // 1             //Objekte für die anliegened Wände
        public GameObject east;    // 2
        public GameObject west;    // 3
        public GameObject south;   // 4

    }

    public GameObject wall;                         //GameObject aus dem die Wände erstellt werden
    public GameObject floor;                        //GameObject aus dem der Boden erstellt wird
    public GameObject cieling;                      //GameObject aus dem die Decke erstellt wird
    public GameObject player;                       //GameObject das den Spieler-Charakter enthält
    public GameObject sobbing;                      //GameObject für extra Sounds
    private GameObject allTheWalls;                 //GameObject damit die Obejekt Leiste nicht mit Klonen zugemüllt wird
    private GameObject allFloor;                    // -----""------
    private GameObject allCieling;                  // -----""------
    private Vector3 intPosition;                    //Position der linken unteren Ecke der Map
    public float wallLength = 1.0f;                 //Länge des Wand-GameObjects
    public int length = 5;                          //Länge der Map (X-Axe)
    public int width = 5;                           //Breite der Map (Z-Axe)
    private Box[] box;                              //Array das alle Boxen beinhaltet
    private int currentBox = 0;                     //Speichert in welcher Box wir uns befinden
    private int checkedBoxes = 0;                   //Anzahl der bereits geprüften Boxen
    private bool started = false;                   //Bool um zu wissen ober es der erste Durchlauf ist oder nicht
    private int currentAdjacent;                    //Anzahl der anliegenden Boxen
    private List<int> lastBox;                      //Stack für den DSF
    private int returnCounter = 0;                  //Counter für den DSF
    private int breakThatWall = 0;                  //int um zu wissen welche Wand gelöscht werden soll

    void Start() {
        checkAudio();                               //Spielt extra Sounds ab
        createWalls();                              //Generiere die Map
        createFloor();                              //Generiert den Boden
        createCieling();                            //Generiert die Decke
        spawnPlayer();                              //Spawnt den Spieler
    }

    void checkAudio() {
        GameObject[] temp = GameObject.FindGameObjectsWithTag("sobbing");           //Sucht nach dem Objekt
        if (temp.Length == 0)                                                       //Wenn es noch nicht in der Szene ist spawnt es das Objekt
        {
            Instantiate(sobbing, new Vector3(0, 0, 0), Quaternion.identity);
        }

    }

    void spawnPlayer() {                                                            //Spawnt den Spieler
        Vector3 spawnPos = new Vector3(((int) Random.Range((-(length/2))+1,(length/2)-1)),-0.3f,((int)Random.Range((-width/ 2)+1, (width / 2)-1)));
        Instantiate(player, spawnPos, Quaternion.identity);

    }

    void createCieling() {                  //einfache nested-for, erstellt ein Dach über dem ganzen Feld 
        allCieling = new GameObject();
        allCieling.name = "Cieling";
        GameObject tempCieling;
        for (float i = (-(length / 2) + 0.5f); i < ((length / 2) + 0.5f); i++)
        {
            for (int o = -(width / 2); o < (width / 2); o++)
            {
                Vector3 pos = new Vector3(i, +1, o);
                tempCieling = Instantiate(cieling, pos, Quaternion.identity) as GameObject;
                tempCieling.transform.parent = allCieling.transform;
            }
        }

    }

    void createFloor(){            //einfache nested-for, erstellt den Boden über dem ganzen Feld 
        allFloor = new GameObject();
        allFloor.name = "Floor";
        GameObject tempFloor;
        for (float i = (-(length/2)+0.5f); i<((length/2)+0.5f); i++) {
            for (int o = -(width/2); o < (width/2); o++) {
                Vector3 pos = new Vector3(i,-1, o);
                tempFloor = Instantiate(floor, pos, Quaternion.identity) as GameObject;
                tempFloor.transform.parent = allFloor.transform;
            }
        }

    }

    void createWalls()
    {
        allTheWalls = new GameObject();
        allTheWalls.name = "Walls";
        intPosition = new Vector3((-length / 2) + (wallLength / 2), 0.0f, (-width / 2) + (wallLength / 2));       //Berechnung der Position der linken unteren Ecke
        Vector3 myPosition = intPosition;
        GameObject tempWall;                                            //Temporäres GameObject zum Zwischenspeichern einer Wand


        for (int i = 0; i < width; i++)
        {                                                              //Die Schleifen erstellen alle Wände der X-Axe entlang
            for (int o = 0; o <= length; o++)
            {
                myPosition = new Vector3(intPosition.x + (o * wallLength) - (wallLength / 2), 0.0f, intPosition.z + (i * wallLength) - (wallLength / 2));
                tempWall = Instantiate(wall, myPosition, Quaternion.identity) as GameObject; //Erstellt die Wand in der Szene
                tempWall.transform.parent = allTheWalls.transform;
            }
        }

        for (int i = 0; i <= width; i++)
        {                                                              //Die Schleifen erstellen alle Wände der Z-Axe entlang
            for (int o = 0; o < length; o++)
            {
                myPosition = new Vector3(intPosition.x + (o * wallLength), 0.0f, intPosition.z + (i * wallLength) - wallLength);
                tempWall = Instantiate(wall, myPosition, Quaternion.Euler(0.0f,90.0f,0.0f)) as GameObject;  //Erstellt die Wand in der Szene
                tempWall.transform.parent = allTheWalls.transform;
            }

        }

        createBoxes();

    }

    void createBoxes() {                                           //Ordnet die Wände den Boxen zu, Code weiß welche Wand zu welcher Box im Raster gehöhrt

        lastBox = new List<int>();
        lastBox.Clear();
        int amount = allTheWalls.transform.childCount;             //Anzahl der Child Objekte
        GameObject[] allWalls = new GameObject[amount];            //Array zum Zwischenspeichern aller Wände
        box = new Box[length * width];
        int counter = 0;                                           //Counter für die Einordnung
        int counter2 = 0;                                          //       -...-
        int counter3 = 0;                                          //       -...-

        for (int i = 0; i < amount; i++) {
            allWalls[i] = allTheWalls.transform.GetChild(i).gameObject;         //Schreibt alle Objekte aus allTheWalls in das Array
        }

        for (int i = 0; i < box.Length; i++) {                                  //Ordnet alle Wände den Boxen zu
            box[i] = new Box();
            box[i].east = allWalls[counter];
            box[i].south = allWalls[counter2+(length+1)*width];
            if (counter3 == length)
            {
                counter = counter + 2;
                counter3 = 0;
            }
            else {
                counter++;

            }

            counter2++;
            counter3++;

            box[i].west = allWalls[counter];
            box[i].north = allWalls[(counter2 + (length + 1) * width)+length-1];

        }

        gridToMaze();

    }

    void gridToMaze() {                             //Löscht Wände um ein Labyrinth zu erstellen (mit Hilfe von DFS)
        while(checkedBoxes < length * width) {
            if (started)
            {
                giveAdjacent();
                if (!box[currentAdjacent].visited && box[currentBox].visited) {
                    breakWall();
                    box[currentAdjacent].visited = true;
                    checkedBoxes++;
                    lastBox.Add(currentBox);
                    currentBox = currentAdjacent;
                    if (lastBox.Count>0) {
                        returnCounter = lastBox.Count - 1;
                    }
                }
            }
            else {
                currentBox = Random.Range(0, (width * length));
                box[currentBox].visited = true;
                checkedBoxes++;
                started = true;
                 }

            

        }
    }

    void breakWall(){                                                           //Zerstört eine Wand um das Labyrinth zu erstellen

        switch (breakThatWall) {
            case 1: Destroy(box[currentBox].north); break;
            case 2: Destroy(box[currentBox].east); break;
            case 3: Destroy(box[currentBox].west); break;
            case 4: Destroy(box[currentBox].south); break;

        }

    }

    void giveAdjacent() {                                                         //Gibt die anliegenden Boxen zurück
        int nAmount = 0;                                                          //Anzahl der gefundenen anliegenden Boxen
        int[] ad = new int[4]; //neighbours                                                    //Array zum speichern der gefundenen Boxen
        int check = ((((currentBox + 1) / length) - 1) * length) + length;        //Wird verwendet um zu überprüfen ob sich die derzeitige Box am Rand befindet
        int[] adWalls = new int[4]; //connectingWall                                               //Werte der anliegenden Wände

        //Rechte Box
        if ((currentBox + 1) < length * width && (currentBox + 1) != check) {
            if (!box[currentBox + 1].visited) {
                ad[nAmount] = currentBox + 1;
                adWalls[nAmount] = 3;
                nAmount++;
            }
        }

        //Linke Box
        if (currentBox - 1 >= 0 && (currentBox) != check)
        {
            if (!box[currentBox - 1].visited)
            {
                ad[nAmount] = currentBox - 1;
                adWalls[nAmount] = 2;
                nAmount++;
            }
        }

        //Obere Box
        if (currentBox + length < length * width)
        {
            if (!box[currentBox + length].visited)
            {
                ad[nAmount] = currentBox + length;
                adWalls[nAmount] = 1;
                nAmount++;
            }
        }

        //Untere Box
        if (currentBox - length >= 0)
        {
            if (!box[currentBox - length].visited)
            {
                ad[nAmount] = currentBox - length;
                adWalls[nAmount] = 4;
                nAmount++;
            }
        }

        
        if (nAmount != 0){                                          //Wenn nichts gefunden wurde, gib einen random Nachbar zurück
            int rand = Random.Range(0, nAmount);
            currentAdjacent = ad[rand];
            breakThatWall = adWalls[rand];
        }else{                                                     //Wenn etwas gefunden wurde, gehe einen Schritt zurück
            if (returnCounter > 0) {
                currentBox = lastBox[returnCounter];
                returnCounter--;
            }
        }

    }

 
    void OnTriggerEnter(Collider other){                //Lädt das Spiel wieder wenn man den Teddy findet
        Application.LoadLevel("Maze");
        
        }
    

}
