# GameJam2023
* Implementation in **Assets/Navigation**

## Runtime:
* Click on the plane. If a path exists, the shortest path is calculated. The capsule will cut corners by raycasting to attempt to skip unnecessary vertex visits.
* Navigation "smoothing" can be toggled on the Player Component for previewing scene gizmos showing the true stair-stepped path
![navSmoothed](https://github.com/patrickdevarney/GameJam2023/assets/11896025/0c4e5e0b-cb57-4811-8060-ddddf07e1c5d)

## Edit time:
* The NavManager object in the scene with the NavManager script on it stores the nav data for the scene and calculates paths.
* The **Bake Settings** can be modified to adjust the size of the nav grid and the granularity of the grid.
* The UI buttons control baking the data and running random path tests that are output in the log.
![image](https://github.com/patrickdevarney/GameJam2023/assets/11896025/a959c02f-3db6-4213-8cae-3691b5e46df6)
