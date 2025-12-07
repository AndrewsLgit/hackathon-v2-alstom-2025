# Guide de Configuration de la Scène VR - Hackathon Alstom

## Vue d'ensemble
Ce guide explique comment configurer correctement la scène VR pour faire fonctionner le système de cubes sur surface.

---

## 1. Structure de la Scène

### Objets requis dans la scène :
1. **XR Origin (XR Rig)** - Pour le système VR
2. **XR Interaction Manager** - Gestion des interactions
3. **Surface** - La surface sur laquelle les cubes seront placés
4. **CubeSpawner** - Script qui génère les cubes
5. **UI Canvas** (optionnel) - Pour les boutons de spawn

---

## 2. Configuration de la Surface

### Préfab Surface (`Assets/Prefabs/Surface.prefab`)

**Important :** La surface DOIT avoir le nom exact "Surface" (sans guillemets) si vous ne l'assignez pas manuellement.

#### Composants requis :
- ✅ `Transform` - Position et orientation
- ✅ `MeshFilter` + `MeshRenderer` - Pour la visualisation
- ✅ `BoxCollider` (optionnel) - Pour la physique

#### Position recommandée :
- **Position Y** : À ajuster selon votre scène (par exemple : `0.5`)
- **Rotation** : Peut être inclinée si nécessaire
- **Scale** : Selon vos besoins (par exemple : `10, 0.1, 10` pour une grande surface)

#### Paramètres importants :
- **Tag** : Peut rester "Untagged"
- **Layer** : Par défaut (0) ou Layer spécifique pour interactions

---

## 3. Configuration du Prefab Cube

### Préfab Cube (`Assets/Prefabs/Cube.prefab`)

#### Composants OBLIGATOIRES :

1. **XRGrabInteractable**
   - ✅ `Select Mode` : Single ou Multiple
   - ✅ `Movement Type` : Velocity Tracking (recommandé) ou Kinematic
   - ✅ `Use Dynamic Attach` : Activé (cochée)
   - ✅ `Match Attach Position` : Activé
   - ✅ `Match Attach Rotation` : Activé (peut être désactivé si lockRotation est activé dans XRClampToSurface)

2. **Rigidbody**
   - ✅ `Mass` : 1-10 (recommandé : 1)
   - ✅ `Drag` : 0-5 (pour le mouvement X/Z)
   - ✅ `Angular Drag` : 0.05-0.5
   - ✅ `Use Gravity` : Peut être activé, sera géré par les scripts
   - ✅ `Is Kinematic` : NON (désactivé)
   - ✅ `Interpolate` : Interpolate ou Extrapolate
   - ✅ `Collision Detection` : Discrete ou Continuous

3. **XRClampToSurface** (script personnalisé)
   - ✅ `Surface` : Assigner la référence à la Surface (ou laisser vide pour auto-détection)
   - ✅ `Offset Y` : **0.1** (DOIT correspondre à `spawnHeight` du CubeSpawner)
   - ✅ `Lock Rotation` : Activé (recommandé)
   - ✅ `Target Rotation` : (0, 0, 0)
   - ✅ `Maintain Height When Not Grabbed` : **false** (comme configuré)

4. **BoxCollider**
   - ✅ Taille adaptée au cube
   - ✅ `Is Trigger` : NON (désactivé)
   - ✅ `Material` : Optionnel (Physic Material)

5. **SurfaceBoundaryDestroyer** (optionnel)
   - ✅ `Surface` : Assigner la référence
   - ✅ `Bounds X` : Limites de la surface sur l'axe X (min, max)
   - ✅ `Bounds Z` : Limites de la surface sur l'axe Z (min, max)
   - ✅ `Tolerance` : 0.1
   - ✅ `Destroy Delay` : 0.5

#### Structure hiérarchique recommandée :
```
Cube (GameObject racine)
├── XRGrabInteractable
├── Rigidbody
├── XRClampToSurface
├── SurfaceBoundaryDestroyer
├── BoxCollider
└── Visuals (enfant)
    ├── MeshFilter
    └── MeshRenderer
```

---

## 4. Configuration du CubeSpawner

### Script CubeSpawner dans la scène

#### Composants requis :
- ✅ `CubeSpawner` (script)

#### Paramètres de configuration :

1. **Cube Prefab**
   - ✅ Assigner le préfab `Cube.prefab` depuis `Assets/Prefabs/`

2. **Surface**
   - ✅ Assigner la référence au GameObject "Surface" dans la scène
   - OU laisser vide pour auto-détection (cherche un objet nommé "Surface")

3. **Spawn Offset** (Vector2)
   - ✅ Position relative au centre de la surface où spawner les cubes
   - Exemple : `(0, 0)` pour spawner au centre

4. **Spawn Height**
   - ✅ **0.1** (DOIT correspondre à `offsetY` du XRClampToSurface)
   - ⚠️ **CRITIQUE** : Cette valeur DOIT être identique à `offsetY` dans le préfab Cube

5. **Max Cubes**
   - ✅ Nombre maximum de cubes simultanés (0 = illimité)
   - Recommandé : 10-20

#### Création dans Unity :
1. Créer un GameObject vide : `GameObject > Create Empty`
2. Le renommer : "CubeSpawner"
3. Ajouter le composant : `Add Component > CubeSpawner`
4. Assigner les références ci-dessus

---

## 5. Synchronisation des Paramètres CRITIQUES

### ⚠️ ATTENTION : Ces valeurs DOIVENT être identiques !

| Composant | Paramètre | Valeur recommandée |
|-----------|-----------|-------------------|
| **XRClampToSurface** (dans Cube prefab) | `offsetY` | **0.1** |
| **CubeSpawner** (dans la scène) | `spawnHeight` | **0.1** |

**Si ces valeurs ne correspondent pas, les cubes spawneront à la mauvaise hauteur et rebondiront !**

---

## 6. Configuration XR Interaction Manager

### Objet : "XR Interaction Manager"

#### Composants :
- ✅ `XRInteractionManager` (automatique)

#### Vérifications :
- ✅ L'objet doit être présent dans la scène
- ✅ Aucune configuration spéciale requise (fonctionne par défaut)

---

## 7. Configuration de l'UI avec Plusieurs Types de Cubes

### Configuration du CubeSpawner pour Plusieurs Prefabs

1. **Dans le CubeSpawner** :
   - `Cube Prefabs` (tableau) : Ajouter 3 prefabs différents
     - Index 0 : Premier type de cube (ex: Cube Rouge)
     - Index 1 : Deuxième type de cube (ex: Cube Vert)
     - Index 2 : Troisième type de cube (ex: Cube Bleu)
   - Le champ `Cube Prefab` (ancien) est conservé pour compatibilité

### Créer 3 Boutons pour Spawner Différents Cubes

1. **Créer un Canvas** :
   - `GameObject > UI > Canvas`
   - Configurer le Canvas Scaler pour VR

2. **Créer 3 Buttons** :
   - `GameObject > UI > Button` (répéter 3 fois)
   - Les renommer : "Button Cube 1", "Button Cube 2", "Button Cube 3"
   - Placer dans le Canvas avec un layout approprié

3. **Configurer chaque Button avec UIButtonSpawner** :

   **Bouton 1** :
   - Sélectionner le premier Button
   - `Add Component > UIButtonSpawner`
   - `Cube Spawner` : Assigner le CubeSpawner (ou laisser vide pour auto-détection)
   - `Spawn Method` : **By Index**
   - `Prefab Index` : **0** (spawnera le premier prefab du tableau)

   **Bouton 2** :
   - Sélectionner le deuxième Button
   - `Add Component > UIButtonSpawner`
   - `Cube Spawner` : Assigner le CubeSpawner
   - `Spawn Method` : **By Index**
   - `Prefab Index` : **1** (spawnera le deuxième prefab du tableau)

   **Bouton 3** :
   - Sélectionner le troisième Button
   - `Add Component > UIButtonSpawner`
   - `Cube Spawner` : Assigner le CubeSpawner
   - `Spawn Method` : **By Index**
   - `Prefab Index` : **2** (spawnera le troisième prefab du tableau)

4. **Alternative - Prefab Spécifique** :
   - Vous pouvez utiliser `Spawn Method: By Prefab` et assigner directement un prefab spécifique à chaque bouton
   - Cela permet plus de flexibilité si vous avez besoin de prefabs différents non dans le tableau

### Méthodes de Spawn Disponibles

- **By Index** : Utilise l'index dans le tableau `cubePrefabs` du CubeSpawner (recommandé pour 3 boutons)
- **By Prefab** : Utilise un prefab spécifique assigné directement sur le bouton

4. **Alternative - Événement Unity** :
   - Sélectionner le Button
   - Dans `On Click()`, ajouter un événement
   - Glisser le CubeSpawner
   - Sélectionner `CubeSpawner > SpawnCube()`

---

## 8. Checklist de Vérification

Avant de tester, vérifiez :

### Surface :
- [ ] Objet nommé "Surface" (ou référence assignée dans les scripts)
- [ ] Position Y correcte
- [ ] Taille appropriée

### Prefab Cube :
- [ ] XRGrabInteractable présent et configuré
- [ ] Rigidbody présent (non-kinematic)
- [ ] XRClampToSurface présent avec `offsetY = 0.1`
- [ ] SurfaceBoundaryDestroyer présent (optionnel)
- [ ] BoxCollider présent

### CubeSpawner :
- [ ] Référence au prefab Cube assignée
- [ ] Référence à la Surface assignée
- [ ] `spawnHeight = 0.1` (identique à offsetY)
- [ ] `spawnOffset` configuré

### Synchronisation :
- [ ] `offsetY` (XRClampToSurface) = `spawnHeight` (CubeSpawner) ✅

---

## 9. Test du Système

### Étapes de test :

1. **Lancez la scène en mode Play**
2. **Spawner un cube** (via UI ou appel direct de `SpawnCube()`)
3. **Vérifiez** :
   - ✅ Le cube apparaît à la bonne hauteur au-dessus de la surface
   - ✅ Pas de bounce lors du spawn
   - ✅ Vous pouvez grab le cube
   - ✅ Le cube reste à la bonne hauteur pendant le grab
   - ✅ Déplacement uniquement sur X et Z
   - ✅ Pas de sauts ou rebonds

### Problèmes courants :

| Problème | Solution |
|----------|----------|
| Cube spawn trop haut/bas | Vérifier que `offsetY` = `spawnHeight` |
| Bounce au spawn | Vérifier la stabilisation dans CubeSpawner |
| Bounce au grab | Vérifier LateUpdate() dans XRClampToSurface |
| Cube tombe après release | `maintainHeightWhenNotGrabbed = false` (comportement normal) |
| Cube ne bouge pas en X/Z | Vérifier XRGrabInteractable > Movement Type |

---

## 10. Paramètres Recommandés

### Pour un comportement optimal :

**XRClampToSurface :**
- `offsetY` : **0.1**
- `lockRotation` : **true**
- `targetRotation` : **(0, 0, 0)**
- `maintainHeightWhenNotGrabbed` : **false**

**CubeSpawner :**
- `spawnHeight` : **0.1**
- `maxCubes` : **10**
- `spawnOffset` : **(0, 0)**

**XRGrabInteractable :**
- `Movement Type` : **Velocity Tracking**
- `Use Dynamic Attach` : **true**

**Rigidbody :**
- `Mass` : **1**
- `Drag` : **2**
- `Use Gravity` : **false** (sera géré par scripts)

---

## Support

En cas de problème, vérifier :
1. Les logs Unity pour les erreurs de scripts
2. Les références assignées dans l'inspecteur
3. La synchronisation `offsetY` = `spawnHeight`
4. Que tous les composants requis sont présents

---

**Dernière mise à jour :** Configuration avec `maintainHeightWhenNotGrabbed = false`

