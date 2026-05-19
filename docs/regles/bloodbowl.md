# Blood Bowl – Règles Bonifiées Saison 3 (Synthèse pour BolDeSangManager)

> Source : `lrb.pdf` (MAJ 11/04/26, "Chroniques d'un Empire Oublié", http://empireoublie.free.fr). Ce document est un extrait condensé fidèle pour le développement. Pour litige, se référer au PDF original.

---

## 1. Caractéristiques des joueurs

Chaque joueur a 5 caractéristiques principales :

| Code | Nom | Description | Type |
|------|-----|-------------|------|
| M | Mouvement | Nombre de cases qu'un joueur peut parcourir lors d'une Action de Mouvement | Valeur (1-9) |
| F | Force | Force physique, base de calcul des dés de blocage | Valeur (1-8) |
| AG | Agilité | Adresse / dextérité — utilisée pour Esquiver, Ramasser, Réceptionner, Intercepter, Bondir | Résultat requis (6+ à 1+) |
| CP | Capacité de Passe | Aptitude à lancer le ballon | Résultat requis (6+ à 1+) |
| AR | Armure | Robustesse — sur Jet d'Armure, le coach adverse doit obtenir ≥ AR pour pénétrer | Résultat requis (3+ à 11+) |

**Valeurs limites :**

| Caractéristique | Min | Max |
|-----------------|-----|-----|
| M | 1 | 9 |
| F | 1 | 8 |
| AG | 6+ | 1+ |
| CP | 6+ | 1+ |
| AR | 3+ | 11+ |

**Tests de caractéristique :**
- Test d'Agilité / Capacité de Passe : jeter 1D6, comparer au résultat requis.
- Jet d'Armure : 2D6 ≥ AR pour pénétrer (le coach adverse jette).
- 1 naturel = toujours échec. 6 naturel = toujours réussite (sauf cas particuliers).
- Améliorations maxi : on ne peut jamais améliorer plus de **2 fois** une caractéristique.

---

## 2. États d'un joueur

| État | Description |
|------|-------------|
| **Debout** | Joueur opérationnel, exerce une Zone de Tacle sur les 8 cases adjacentes. |
| **Déconcentré** | Debout mais sans Zone de Tacle, ne peut utiliser Compétences/Traits Actifs, ne peut Intercepter ni Réceptionner. Pion bleu. |
| **À Terre** | Joueur au sol ; coûte 3 M pour se relever. Pas de ZdT. Pion jaune. |
| **Sonné** | Pion rouge. Ne peut être activé. À la fin du tour de son équipe, redevient À Terre. |
| **KO** | Hors du terrain (Box des Joueurs KO). À chaque fin de Phase : 4+ sur 1D6 → retour en Réserves. |
| **Blessé / Éliminé** | Hors du terrain (Box des Joueurs Éliminés). Le coach adverse fait un Jet d'Élimination. |
| **Réserve** | Box des Réserves, prêt à être placé à la prochaine Phase. |
| **Enraciné** | Pour les Homme-arbres (Trait Prendre Racine). Ne peut bouger. |
| **Croqué** | Cible immobilisée par le Trait Grande Gueule. |

**Mises à terre :**
- **Mis À Terre** : pas de Jet d'Armure (ex. via effet d'événement).
- **Chuter** : Jet d'Armure + Turnover si équipe active.
- **Plaqué** : Jet d'Armure + Turnover si équipe active.

Quand un joueur en possession du ballon Mis À Terre / Chute / Plaqué → le ballon Rebondit.

---

## 3. Séquence d'un match

- **Match** = 2 mi-temps, chacune de 8 rounds = 16 tours par équipe et par match.
- **Round** = 1 tour pour chaque coach.
- **Phase** = période entre un coup d'envoi et un touchdown (ou la fin de mi-temps).

### 3.1 Séquence d'Avant Match (Jeu en Ligue)
1. Les Fans (Facteur de Popularité = Fans Dévoués + 1D3 Fans Occasionnels)
2. La Météo (2D6, voir table)
3. Prendre des Journaliers (si <11 joueurs disponibles)
4. Coups de Pouce (Trésorerie + Petite Monnaie)
5. Tirer au dé pour déterminer qui engage

### 3.2 Séquence de Début de Phase
1. **Placement** : l'équipe qui engage en premier ; 11 joueurs max ; ≥3 sur la Ligne d'Engagement (Champ Centre) ; ≤2 par Couloir Latéral.
2. **Coup d'Envoi** : un joueur de l'équipe qui engage donne le coup d'envoi sur une case de la moitié adverse.
3. **Le ballon Dévie** (1D6 cases dans direction 1D8).
4. **Événement de Coup d'Envoi** (2D6, table ci-dessous).
5. **Atterrissage** : si case occupée → Test d'Agilité pour Réceptionner. Sinon, Rebond.

### 3.3 Tableau de Météo (2D6)

| 2D6 | Météo | Effet |
|-----|-------|-------|
| 2 | Canicule | À fin de chaque Phase : 1D3 joueurs au hasard de chaque équipe → Box des Réserves, ratent la Phase suivante. |
| 3 | Très Ensoleillé | -1 aux Tests de CP. |
| 4-10 | Conditions Idéales | Aucun effet. |
| 11 | Averse | -1 aux jets pour Ramasser, Réceptionner, Intercepter. |
| 12 | Blizzard | -1 supplémentaire pour Foncer ; uniquement passes Rapides ou Courtes. |

### 3.4 Tableau des Événements de Coup d'Envoi (2D6)

| 2D6 | Nom | Effet |
|-----|-----|-------|
| 2 | À mort l'Arbitre | Chaque équipe reçoit 1 Pot-de-vin gratuit (perdu si non utilisé). |
| 3 | Temps Mort | Si l'équipe qui engage est au tour 6/7/8 : recule les 2 marqueurs de tour. Sinon, avance les 2 marqueurs. |
| 4 | Solide Défense | L'équipe qui engage replace D3+3 de ses joueurs Démarqués. |
| 5 | Chandelle | 1 joueur Démarqué de l'équipe qui reçoit peut se placer sur la case où le ballon atterrit. |
| 6 | Fans en Folie | Chaque coach : 1D6 + Cheerleaders. Le meilleur (les 2 si égalité) gagne +1 Soutien Offensif au prochain Blocage. |
| 7 | Coaching Brillant | Chaque coach : 1D6 + Coachs Assistants. Le meilleur (les 2 si égalité) gagne 1 Relance gratuite pour la Phase. |
| 8 | Météo Capricieuse | Refaire un jet sur le Tableau de Météo. Si Conditions Idéales → le ballon Valdingue (3). |
| 9 | Surprise | L'équipe qui reçoit : jusqu'à D3+3 joueurs Démarqués peuvent bouger d'1 case. |
| 10 | Charge ! | L'équipe qui engage : D3+3 joueurs Démarqués peuvent être activés (Mouvement gratuit + 1 Blitz, 1 Lancer de Coéquipier, 1 Botter de Coéquipier). Si Chute/Plaqué → la Charge s'arrête. |
| 11 | En-cas Suspect | Chaque coach : 1D6. Le plus mauvais (ou les 2) désigne 1 joueur au hasard. Sur 2+ : -1 M, -1 AR pour la Phase. Sur 1 : passe la Phase aux toilettes. |
| 12 | Invasion du Terrain | Chaque coach : 1D6 + FP. Le plus mauvais (ou les 2) : D3 joueurs au hasard sont Mis à Terre + Sonnés. |

> Note : LRB Saison 3 utilise officiellement un D16 pour ce tableau dans certaines sources, mais la version condensée du document source est 2D6.

### 3.5 Fin de Phase

Séquence dans l'ordre :
1. Gestion des Armes Secrètes (joueurs avec Trait Arme Secrète → Expulsés).
2. Effets de Fin de Phase (météo, etc.).
3. Rétablissement des Joueurs KO : 4+ sur 1D6 → Réserves.
4. La Phase prend fin.

Si match continue → **Reprendre le Match** (nouvelle Phase). Sinon → **Fin du Match**.

### 3.6 Égalité / Prolongations / Tirs au But
- Prolongations : 8 tours supplémentaires, pas de récupération de relances.
- Tirs au But : 5 tirs au dé (1D6 chacun), le meilleur cumulé gagne.

---

## 4. Tour d'équipe et Actions

Pendant son tour, le coach active ses joueurs un à un (sauf ceux Sonnés au début du tour) jusqu'à activation complète ou Turnover.

### 4.1 Actions disponibles à l'activation

| Action | Limite/Tour | Description |
|--------|-------------|-------------|
| **Mouvement** | Illimité | Se déplace jusqu'à M cases. |
| **Sécurisation du Ballon** | 1 | Mouvement gratuit + ramassage sécurisé (2+ auto, pas de Test d'Agilité). Nécessite : aucun adversaire Debout non-Déconcentré à ≤2 cases du ballon. Interdit aux Gros Bras et Instable. |
| **Blocage** | Illimité | Bloque un adversaire Debout adjacent. |
| **Blitz** | 1 | Mouvement + 1 Blocage (coûte 1 point de M). |
| **Passe** | 1 | Mouvement gratuit puis Passe (pas de mouvement après). |
| **Transmission** | 1 | Mouvement gratuit puis transmet le ballon à un coéquipier Debout adjacent. |
| **Lancer de Coéquipier** | 1 | Mouvement gratuit puis lance un coéquipier Poids Plume adjacent. Nécessite Trait Lancer de Coéquipier. |
| **Agression** | 1 | Mouvement gratuit puis Jet d'Armure contre un joueur adverse À Terre/Sonné adjacent. |
| **Actions Spéciales** | Variable | Définies par Compétences/Traits. |

### 4.2 Turnover (fin prématurée du tour)

Causes principales :
- Joueur actif Chute.
- Joueur actif Plaqué (par l'adversaire pendant son tour).
- Joueur actif en possession du ballon Mis À Terre / sort du terrain.
- Échec à ramasser le ballon.
- Maladresse sur Passe.
- Échec de Réception avec ballon au sol.
- Passe sans réception côté équipe active.
- Joueur adverse intercepte.
- Lancer de Coéquipier qui mange / sort du terrain (avec ballon).
- Joueur actif Expulsé pour Agression.
- **Touchdown** marqué.

### 4.3 Se relever
- Coûte **3 M**.
- Si M ≤ 2 : 1D6 ≥ 4 ; sinon reste À Terre et activation prend fin.

### 4.4 Esquiver
- Quand un joueur quitte une case où il est Marqué.
- Test d'Agilité, avec -1 par joueur adverse marquant la case de **destination**.
- Échec → Chute.

### 4.5 Bondir
- Saute par-dessus un joueur À Terre/Sonné vers une case inoccupée non-adjacente.
- Coûte 2 M.
- Test d'Agilité, modificateur = - (joueurs adverses marquant case de départ OU case d'arrivée, le plus élevé des deux).
- Échec → Chute sur case d'arrivée. 1 naturel → Chute sur case de départ.

### 4.6 Ramasser le ballon
- Test d'Agilité, -1 par joueur adverse marquant le joueur.
- Échec → Turnover et Rebond.

### 4.7 Foncer
- Permet 1 ou 2 cases supplémentaires en fin de Mouvement.
- Chaque tentative : 1D6 ≥ 2. Sur 1 → Chute.

---

## 5. Blocage

### 5.1 Calcul des dés
Comparer la Force modifiée des deux joueurs (avec Soutiens) :
- Égale → 1 dé.
- Plus forte → 2 dés (le coach du plus fort choisit le résultat).
- Plus du double → 3 dés (le coach du plus fort choisit).

### 5.2 Soutiens
- **Soutien Offensif** (+1 F au bloqueur) : coéquipier qui Marque la cible **et** n'est marqué par aucun joueur adverse.
- **Soutien Défensif** (+1 F à la cible) : coéquipier de la cible qui Marque le bloqueur **et** n'est marqué par aucun joueur adverse.

### 5.3 Résultats des dés de Blocage

| Icône | Nom | Effet |
|-------|-----|-------|
| Attaquant Plaqué | Le bloqueur est Plaqué. |
| Les Deux Plaqués | Les deux joueurs sont Plaqués. (Compétences `Blocage`/`Lutte` modifient ce résultat.) |
| Repoussé | La cible est Repoussée de 1 case. Le bloqueur peut Poursuivre. |
| Bousculé | Si la cible a `Esquive` → Repoussé. Sinon → Défenseur Plaqué. |
| Défenseur Plaqué | Repoussé + Plaqué. |

### 5.4 Repousser et Poursuivre
- Direction définie par les schémas (3 cases possibles selon orientation).
- Coach actif choisit la case (préférer inoccupée).
- **Poussée à la Chaîne** : si toutes cases occupées, Pousse un autre joueur en cascade.
- **Poussé dans le Public** : si bord de terrain, risque d'être Blessé par le Public + ballon Renvoyé + Turnover si actif.
- **Poursuivre** : le bloqueur peut se déplacer gratuitement sur la case libérée (avant Jet d'Armure éventuel).

---

## 6. Armure et Blessures

### 6.1 Jet d'Armure
2D6 ≥ AR du joueur → armure pénétrée → Jet de Blessure.

### 6.2 Tableau de Blessure (2D6, standard)

| 2D6 | Résultat |
|-----|----------|
| 2-7 | Sonné |
| 8-9 | KO (Box KO) |
| 10-12 | Éliminé (Jet d'Élimination par l'adversaire) |

### 6.3 Tableau de Blessure de Minus (joueurs avec Trait Minus)

| 2D6 | Résultat |
|-----|----------|
| 2-6 | Sonné |
| 7-8 | KO |
| 9 | Commotion (Élimination directe, pas de Jet d'Élimination) |
| 10-12 | Éliminé (Jet d'Élimination) |

### 6.4 Tableau d'Élimination (D16)

| D16 | Résultat | Effet long terme |
|-----|----------|------------------|
| 1-8 | Commotion | Aucun |
| 9-10 | Amoché | Rate le Prochain Match (RPM) + Revanche possible |
| 11-12 | Blessure Sérieuse | Blessure Persistante (BP) + RPM + Revanche |
| 13-14 | Séquelle | Réduction de Caractéristique + RPM + Revanche |
| 15-16 | Mort | Retiré de la Liste d'Équipe |

### 6.5 Tableau de Séquelle (1D6)

| 1D6 | Séquelle | Réduction |
|-----|----------|-----------|
| 1-2 | Traumatisme Crânien | -1 AR |
| 3 | Genou Déboîté | -1 M |
| 4 | Bras Cassé | -1 CP |
| 5 | Hanche Disloquée | -1 AG > ⚠️ Le document mentionne aussi "Cou Brisé : -1 AG" et "Épaule Disloquée : -1 F" à des emplacements différents (p. 42 vs feuille de référence p. 115). La table principale (p. 42) donne : 1-2 Traumatisme Crânien / 3 Genou Déboîté / 4 Bras Cassé / 5 Hanche Disloquée / 6 Cou Brisé. Les caracs réduites correspondantes sont : -1 AR / -1 M / -1 CP / -1 AG / -1 F |
| 6 | Cou Brisé | -1 F |

### 6.6 Blessure Persistante (BP)
- +1 au Jet d'Élimination pour chaque BP du joueur (cumul).
- Hors saison : 1D6 (+1 si Apothicaire) ; 4+ → guérison.

### 6.7 Apothicaire
- 1 utilisation par match.
- Rafistole un KO → Sonné (sauf Blessé par le Public → Box Réserves).
- Rafistole un Éliminé → relance du Jet d'Élimination + choix du résultat. Si Commotion choisi → Box des Réserves.

### 6.8 Revanche
- Si Amoché/Blessure Sérieuse/Séquelle en Ligue : 4+ sur 1D6 → gagne `Haine (X)` (X = un mot-clé de l'agresseur, sauf postes génériques).

### 6.9 Expulsion (Agression)
- Double naturel sur Jet d'Armure ou Jet de Blessure pendant une Agression → Expulsé + Turnover.
- **Contester la Décision** : 1D6. 1 = "Dégagez !" (coach ne peut plus contester) ; 2-5 = "Je m'en fiche !" ; 6 = annule l'Expulsion (mais Turnover demeure).

---

## 7. Passe / Transmission / Lancer de Coéquipier

### 7.1 Séquence d'une Action de Passe
1. Annoncer la case cible (entièrement à portée maximum de la Réglette).
2. Mesurer la portée.
3. Tester la Précision (1D6 + modificateurs vs CP).
4. Interceptions possibles.
5. Résoudre.

### 7.2 Portées de Passe et modificateurs CP

| Portée | Modif CP |
|--------|----------|
| Passe Rapide (I) | 0 |
| Passe Courte (II) | -1 |
| Passe Longue (III) | -2 |
| Longue Bombe (IIII) | -3 |
| Par joueur adverse marquant le passeur | -1 (chacun) |

### 7.3 Résultats de Passe
- **Passe Précise** : réussite ou 6 naturel → ballon atterrit sur case cible.
- **Passe Imprécise** : échec → ballon Valdingue (3) depuis la cible.
- **Maladresse sur Passe** : 1 naturel ou jet ≤ 1 modifié → ballon Rebondit depuis le passeur, Turnover.

### 7.4 Interception
Si la Réglette chevauche un joueur adverse Debout (avec ZdT), il peut tenter :
- Test d'Agilité avec -3 (Précise) ou -2 (Imprécise), -1 par joueur marquant.
- Réussite ou 6 naturel → joueur prend possession, Turnover.

### 7.5 Réception
Test d'Agilité avec :
- -1 si ballon a Rebondi.
- -1 si Renvoi.
- -1 par joueur adverse Marquant.
- À Terre / Sonné / Déconcentré → échec automatique.

### 7.6 Renvoi (ballon hors terrain)
- Place Gabarit de Renvoi sur dernière case du terrain.
- 1D6 → direction.
- 2D6 → distance (cases parcourues).
- Si Renvoi d'angle : Gabarit de Direction Aléatoire + 1D3 distance.

### 7.7 Action de Transmission
- Mouvement gratuit, puis donne à un coéquipier Debout adjacent (non-Déconcentré).
- Le coéquipier fait un Test d'Agilité pour Réceptionner.

### 7.8 Action de Lancer de Coéquipier
- Nécessite Trait `Lancer de Coéquipier` (lanceur) + Trait `Poids Plume` (coéquipier lancé).
- Portées : Lancer Rapide (I, mod 0), Lancer Court (II, mod -1).
- Résultats : Lancer Superbe (Précis), Lancer Médiocre (échec, malus à l'atterrissage), Maladresse sur Lancer (rebond depuis lanceur).
- Atterrissage : Test d'Agilité, -1 si Lancer Médiocre ou Maladresse, -1 par joueur adverse marquant la case d'arrivée. À Terre/Sonné/Déconcentré quand lancé → échec auto.
- Atterrissage sur case occupée : joueur sur la case automatiquement Plaqué, le lancé Rebondit puis Chute.

---

## 8. Touchdown et Score

Pour marquer un Touchdown :
- Joueur Debout en possession du ballon, sur une case de la Zone d'En-but adverse.
- Possible pendant le tour de l'adversaire (Repoussé, Réception, etc.) → Tour adverse prend fin + l'équipe qui a marqué saute son tour suivant.
- Marquer un Touchdown = Turnover + Fin de Phase.

**Temporiser** : si un joueur en possession peut marquer sans jet de dé et ne le fait pas, il Temporise. À la fin de son activation : 1D6. Si ≥ tour actuel → projectile du public, joueur Plaqué + Turnover.

**Concéder le Match** : abandon volontaire. Conséquences :
- Adversaire gagne 2-0 (ou plus).
- Pas de JDM côté concédant.
- Adversaire reçoit 2 JDM.
- Aucun Gain pour le concédant ; l'adversaire gagne Affluence + TDs × 10 000.
- Tous les PSP du match perdus.
- Fans Dévoués -D3 (min 1).
- Pour chaque joueur ≥ 3 améliorations : 1D6 ; 1-3 → quitte l'équipe.

**Concéder sans Pénalité** (si <3 joueurs placables sur Ligne d'Engagement) : 2-0 mais pas de pénalité additionnelle.

---

## 9. Compétences et Traits

**Compétence Active** : utilisable seulement si Debout et non-Déconcentré.
**Compétence Passive** : toujours active.
**Astérisque (*)** : doit toujours être utilisée si applicable.
**Compétence d'Élite (E)** : +10 000 po à la Valeur du joueur quand acquise.

Catégories : A=Agilité, F=Force, G=Générale, M=Mutation, P=Passe, S=Scélérate. + Traits (innés).

### 9.1 Compétences d'Agilité (A)

| Nom | Type | Description |
|-----|------|-------------|
| Défenseur | Active | Adversaires marqués par ce joueur ne peuvent pas utiliser Garde ni Coup de Crampons pendant leurs tours. |
| Équilibre | Active | Une fois par tour, relance d'1D6 lors d'une tentative de Foncer. |
| Esquive | Active – Élite | Une fois par tour, relance d'un Test d'Agilité raté en Esquivant. De plus, transforme Bousculé en Repoussé sur les dés de Blocage. |
| Frappe-et-Court | Active | Après Action de Blocage / Action Spéciale de Poignard réussie, déplacement gratuit d'1 case en ignorant ZdT. Aucune Marquer/Marqué après. Incompatible avec Frénésie. |
| Glissade Contrôlée | Active | Quand Repoussé, c'est le coach du joueur qui choisit la case adjacente inoccupée. |
| Libération Contrôlée | Active | Quand Plaqué/Chute/Mis À Terre avec ballon, peut placer le ballon sur une case adjacente inoccupée au lieu du Rebond. |
| Réception | Active | Relance du Test d'Agilité raté lors d'une Réception. |
| Réception Plongeante | Active | Peut tenter de Réceptionner un ballon atterrissant dans sa ZdT (sauf si suite à Rebond). +1 au Test d'Agilité s'il est sur la case cible. |
| Rétablissement | Active | Peut se relever gratuitement (sans coûter 3 M). De plus, peut annoncer une Action de Blocage À Terre : Test d'Agilité +1 ; réussite → se relève et bloque ; échec → reste À Terre, activation finit. |
| Saut | Active | Peut Sauter par-dessus une case adjacente (peu importe contenu). Modificateurs négatifs réduits de 1 (min -1). Incompatible avec Monté sur Ressort. |
| Sprint | Active | Peut Foncer 3 fois au lieu de 2. |
| Tacle Plongeant | Active | Quand un adversaire tente de quitter sa ZdT par Esquive/Saut/Bond, après son jet, applique -2 au Test et place le joueur À Terre sur case quittée. |

### 9.2 Compétences de Force (F)

| Nom | Type | Description |
|-----|------|-------------|
| Bagarreur | Active | Relance d'un résultat Les Deux Plaqués sur une Action de Blocage. |
| Blocage Multiple | Active | Peut effectuer 2 Actions de Blocage à -2 F, contre 2 joueurs Marqués différents. Pas de Poursuite. Incompatible Frénésie. |
| Bras Musclé | Active | +1 Test de CP lors d'un Lancer de Coéquipier (nécessite Trait Lancer de Coéquipier). |
| Châtaigne | Active – Élite | Quand Plaque un adversaire, +1 au Jet d'Armure OU au Jet de Blessure (après jet). |
| Clé de Bras | Active | Si adverse Chute en Esquivant/Sautant/Bondissant depuis ZdT, +1 au Jet d'Armure ou Jet de Blessure. Élimination compte pour PSP. |
| Crâne Épais | Passive | Sur Jet de Blessure, KO seulement sur 9 (8 devient Sonné). Avec Minus : 7 devient Sonné, KO seulement sur 8. |
| Dans le Mille | Active | Sur Lancer de Coéquipier avec Lancer Superbe, le joueur lancé ne Valdingue pas (atterrit directement sur case cible). |
| Esquive en Force | Active | Une fois par tour, +1/+2/+3 au Test d'Agilité d'Esquive selon F (≤3 / 4 / ≥5). |
| Frappe Précise | Active | Quand désigné joueur qui engage, coup d'envoi Dévie D3 cases au lieu de D6. |
| Frénésie* | Active | Si la cible d'un Blocage est Repoussée, doit Poursuivre et effectuer un 2e Blocage contre elle (si toujours Debout). Pendant Blitz : coûte 1 M supp ; Foncer obligatoire si nécessaire. Incompatible avec Projection, Frappe-et-Court, Blocage Multiple. |
| Garde | Active – Élite | Peut fournir Soutien Offensif/Défensif quel que soit le nombre de joueurs adverses Marquant. |
| Juggernaut | Active | Pendant Blitz, transforme Les Deux Plaqués en Repoussé. Adversaires ne peuvent utiliser Parade/Stabilité/Lutte. |
| Projection | Active | Au Blocage, peut choisir n'importe quelle case adjacente inoccupée pour Repousser la cible. Annule Glissade Contrôlée adverse. Incompatible Frénésie. |
| Stabilité | Active | Peut choisir de ne pas être Repoussé. |

### 9.3 Compétences Générales (G)

| Nom | Type | Description |
|-----|------|-------------|
| Appuis Sûrs | Active | Quand Plaqué/Chute : 1D6 ; sur 6 → ne tombe pas. Pendant son activation, peut continuer + pas de Turnover. |
| Arracher le Ballon | Active | Au Blocage contre porteur, si Repoussé → fait tomber le ballon (qui Rebondit) avant que cible soit À Terre. |
| Blocage | Active – Élite | Ne tombe pas sur résultat Les Deux Plaqués. |
| Frénésie | (cf Force) | |
| Intrépide | Active | Si l'adversaire a F plus élevée : 1D6 + sa F. Si > F brute de l'adversaire, sa F est égalisée à celle de l'adversaire pour ce Blocage. |
| Lutte | Active | Sur Les Deux Plaqués, les 2 joueurs sont Mis À Terre (au lieu de Plaqués). |
| Parade | Active | Si Repoussé suite à Blocage adverse, l'adversaire ne peut pas Poursuivre. Inefficace contre Chaîne & Boulet et contre Juggernaut en Blitz. |
| Prise Sûre | Active | Relance le D6 pour Ramasser (pas pour Sécurisation du Ballon). Annule la Compétence Arracher le Ballon contre lui. |
| Pro | Active | Une fois par activation, peut tenter de relancer un dé (de son équipe) sur 3+ avec 1D6. Pas pour Armure / Blessure / Élimination / hors activation. |
| Provocation | Active | Si Repoussé suite à Blocage adverse, peut forcer l'adversaire à Poursuivre. Inefficace contre Enraciné. |
| Tacle | Active | Adversaire dans sa ZdT ne peut utiliser Esquive (compétence). De plus, sur Bousculé en Blocage, la cible ne compte pas comme ayant Esquive. |

### 9.4 Compétences de Mutation (M)

| Nom | Type | Description |
|-----|------|-------------|
| Bras Supplémentaires | Active | +1 au Test d'Agilité pour Réceptionner / Ramasser / Intercepter. |
| Cornes | Active | +1 F lors des Blocages effectués durant un Blitz. |
| Deux Têtes | Active | +1 au Test d'Agilité pour Esquiver. |
| Grande Gueule | Active | Action Spéciale de Croquement (1D6) : 1-2 rien ; 3+ → adversaire Debout Marqué devient Croqué (ne peut quitter sa case tant que Marqué). Empêche Arracher le Ballon contre lui. |
| Griffes | Passive | Sur Jet d'Armure suite à un Blocage de ce joueur, tout jet ≥ 8 naturel pénètre l'armure. |
| Main Démesurée | Active | Ignore tous les modificateurs négatifs pour Ramasser le ballon. |
| Peau de Fer | Passive | Adversaires ne peuvent appliquer aucun modificateur sur Jet d'Armure contre lui. Annule Griffes contre lui. |
| Présence Perturbante* | Passive | Adversaires à ≤3 cases : -1 par joueur ayant cette compétence sur Test de CP ou Test d'Agilité pour Passe/Lancer/Bombe/Interception/Réception. |
| Queue Préhensile | Active | -1 supplémentaire au Test d'Agilité quand un adversaire Esquive/Bondit/Saute depuis sa ZdT. |
| Répulsion* | Passive | Quand un adversaire tente un Blocage / Action Spéciale ciblée : 1D6. Sur 1 → action annulée, activation prend fin. |
| Tentacules | Active | Quand adversaire Esquive/Bondit/Saute depuis sa ZdT : 1D6 + sa F – F adverse. ≥6 ou 6 naturel → adversaire reste sur place, activation finit. |
| Très Longues Jambes | Active | +1 au Test d'Agilité pour Sauter/Bondir, +2 pour Intercepter. Ignore Perce-Nuages. |

### 9.5 Compétences de Passe (P)

| Nom | Type | Description |
|-----|------|-------------|
| Canonnier | Active | +1 au Test de CP pour Passe Longue ou Longue Bombe. |
| Chef | Active | Une équipe avec ≥1 joueur Chef en début de mi-temps gagne 1 Relance de Chef (perdue si tous les Chefs retirés avant usage). |
| Dégagement | Active | Action Spéciale Dégagement : botte le ballon (1 par tour). Gabarit Renvoi + 1D6 direction + 1D6 distance. Sur Frappe Précise : relance des dés. |
| Délestage | Active | Quand un adversaire tente un Blocage / Action Spéciale ciblée, peut effectuer une Passe Rapide gratuite avant résolution (pas de Turnover possible sur la Passe). |
| Nerfs d'Acier | Active | Peut ignorer certains/tous les modificateurs pour être Marqué dans Tests d'Agilité Réception ou Tests de CP Passe. |
| Passe | Active | Relance d'un Test de CP raté sur Action de Passe. |
| Passe Assurée | Active | Sur 1 naturel à un Test de CP, pas de Maladresse : reste en possession, activation finit, pas de Turnover. |
| Passe Désespérée | Active | Peut viser n'importe quelle case (pas besoin de Réglette), traité comme Longue Bombe. Toute Précise devient Imprécise. Pas d'Interception possible. |
| Perce-Nuages | Active | Sur ses Actions de Passe, adversaires ne peuvent pas Intercepter. |
| Précision | Active | +1 au Test de CP pour Passe Rapide ou Courte. |
| Sur le Ballon | Active | Quand adversaire annonce une Passe, après case cible, peut se déplacer de ≤3 cases (pas Foncer). Aussi : en début de Phase après Déviation, 1 joueur Démarqué avec cette compétence peut se déplacer de ≤3 cases dans sa moitié. |
| Transmission dans la Course | Active | Action de Passe Rapide ou Transmission → activation continue avec mouvement restant (sauf Turnover). |

### 9.6 Compétences Scélérates (S)

| Nom | Type | Description |
|-----|------|-------------|
| Agresseur Solitaire | Active | Sur Agression sans aucun Soutien, relance d'un Jet d'Armure raté. |
| Agression Éclair | Active | Action d'Agression ne finit pas l'activation, continue avec mouvement restant. |
| Coup de Crampons | Active | Soutien Offensif sur Agression d'un coéquipier, quel que soit nombre d'adversaires marquant. |
| Fourchette | Active | Quand un adversaire est Repoussé par lui, il ne peut plus fournir de Soutien jusqu'à la fin de sa prochaine activation. |
| Fumblerooski | Active | Pendant Action de Mouvement avec ballon, peut le déposer sur n'importe quelle case quittée. Pas de Turnover. |
| Innovateur Violent | Active | Reçoit les PSP pour Éliminations via Actions Spéciales (requiert un Trait permettant une Action Spéciale). |
| Joueur Déloyal | Active | Action d'Agression : +1 au Jet d'Armure ou Blessure (après jet). |
| Marteau-pilon | Active | Quand un adversaire est Plaqué par un Blocage, peut faire une Action d'Agression gratuite contre lui (si toujours Debout et Marqué). Puis Mis À Terre + activation finit. |
| Poursuite | Active | Quand adversaire Esquive hors de sa ZdT : 1D6. Sur 4+ → suit l'adversaire sur sa case libérée. Utilisable jusqu'à M fois par tour. |
| Saboteur | Active | Si Plaqué suite à Blocage adverse : 1D6 avant Jet d'Armure. Sur 4+ → arme sabotée explose, adversaire aussi Plaqué (pas de Turnover sauf si porteur). Nécessite Trait Arme Secrète. |
| Sournois | Active | Pas d'Expulsion sur double naturel d'Action d'Agression si armure non pénétrée. |
| Vol Fatal | Active | Quand lancé via Lancer de Coéquipier et atterrit sur case occupée par adversaire Plaqué, +1 au Jet d'Armure ou Blessure. PSP pour Élimination. Nécessite Trait Poids Plume. |

### 9.7 Traits (innés, non-apprenables)

| Nom | Type | Description |
|-----|------|-------------|
| Animosité (X)* | Actif | Quand Passe/Transmission vers coéquipier ayant mot-clé X : 1D6. Sur 1 → refuse, activation finit. `Animosité (tous)` = vise tous coéquipiers. |
| Arme Secrète* | Actif | Expulsé à la fin de chaque Phase où il a participé. |
| Bombardier | Actif | Action Spéciale Lancer de Bombe (1/tour) ; suit règles d'une Passe. La bombe explose sur case d'atterrissage : Plaque les Debout + Jet d'Armure aux adjacents sur 4+. Maladresse / Réception ratée → explose sur case. |
| Botter de Coéquipier | Passif | Action Spéciale Botter de Coéquipier (1/tour) ; règles du Lancer de Coéquipier ; ne compte pas comme Lancer de Coéquipier du tour. Maladresse → Jet de Blessure (Sonné devient KO). |
| Cerveau Lent* | Passif | Après annonce d'action : 1D6. 2+ → action normale. 1 → Déconcentré. |
| Chaîne et Boulet* | Actif | Seule action possible : Action Spéciale Chaîne & Boulet : Gabarit de Renvoi + 1D6 → se déplace dans direction. Esquive auto. Plaqué/Chute/Mis À Terre → Jet de Blessure (Sonné→KO). Incompatible avec Tacle Plongeant, Fourchette, Frénésie, Projection, Frappe-et-Court, Saut, Blocage Multiple, Sur le Ballon, Poursuite, Appuis Sûrs. |
| Contagieux | Passif | 1/match, si inflige Mort via Blocage sans Apothicaire/Régénération : ajoute 1 nouveau Trois-quart au Box des Réserves. Embauche permanente possible en Après-Match (gratuit). Inopérant contre Gros Bras, Décomposition, Régénération, Minus. |
| Décomposition* | Passif | +1 à tout Jet d'Élimination contre lui. |
| Farceur | Actif | Quand un adversaire annonce Blocage / Action Spéciale ciblée : peut se replacer sur une case adjacente à l'attaquant avant les jets. Si porteur dans En-but adverse, action résolue puis Touchdown. |
| Fureur Débridée* | Passif | Après annonce d'action : 1D6 (+2 si Blocage/Blitz). 4+ → action OK. 1-3 → activation finit (sans autre effet). |
| Gerbe de Vomi | Actif | Action Spéciale Gerbe de Vomi : 1D6 ; 2+ → cible adjacente Debout subit Jet d'Armure non-modifié + Blessure si pénétrée. 1 → lui-même subit Jet d'Armure. |
| Gros Débile* | Passif | Après annonce : 1D6 (+2 si coéquipier Debout adjacent non-Déconcentré non-Gros Débile). 4+ → action OK. 1-3 → Déconcentré. |
| Haine (X)* | Passif | Relance d'un Attaquant Plaqué quand bloque un joueur avec mot-clé X. |
| Insignifiant* | Passif | Ne peut pas être en surnombre vs joueurs sans ce Trait dans la Liste. |
| Instable* | Passif | Ne peut pas Sécuriser le Ballon. |
| Ivrogne* | Passif | -1 aux tests pour Foncer. |
| Lancer de Coéquipier | Passif | Peut annoncer l'Action Lancer de Coéquipier. |
| Microbe* | Passif | +1 au Test d'Agilité d'Esquive. Mais n'applique pas le -1 pour Marquer un adverse qui Esquive depuis sa ZdT. |
| Minus* | Passif | Pas de modificateur négatif pour Marquage lors d'Esquive. -1 au Test d'Agilité pour Intercepter. Utilise Tableau de Blessure de Minus. |
| Mon Ballon* | Passif | Ne peut pas lâcher volontairement le ballon : pas de Passe / Transmission / abandon. |
| Monté sur Ressort | Actif | Peut s'Élancer par-dessus 1 case (comme Bondir, mais ignore tous les modificateurs négatifs). Incompatible Saut. |
| Petit Remontant | Actif | À la fin de chaque tour adverse, 1D6 pour chaque coéquipier À Terre à ≤3 cases d'un joueur Debout avec ce Trait. 5+ → se relève. |
| Piqué | Actif | Quand lancé via Lancer de Coéquipier, peut choisir de ne pas Valdinguer : Gabarit Renvoi + 1D6 direction + 1D6 distance. Relance possible du Test d'Atterrissage. |
| Poids Plume* | Passif | Peut être lancé par coéquipier Lancer de Coéquipier, même À Terre. |
| Poignard | Actif | Action Spéciale Poignard : cible adjacente Debout → Jet d'Armure non-modifié + Blessure si pénétrée. |
| Prendre Racine* | Passif | Après annonce, si Debout : 1D6. 2+ → action OK. 1 → Enraciné (ne peut bouger, ne peut Poursuivre, ne peut être Repoussé). Fin de Phase ou Plaqué/Mis À Terre → fin de l'Enraciné. |
| Regard Hypnotique | Actif | Action Spéciale Regard Hypnotique : 1D6. 1-2 → rien, activation finit. 3+ → cible Debout adjacente devient Déconcentrée. |
| Régénération | Passif | Quand Éliminé, 1D6 avant Jet d'Élimination. 4+ → ignore l'Élimination (mais PSP comptent), placé en Réserves. |
| Sans Ballon* | Passif | Ne peut jamais être en possession. Réception/Ramassage/Interception échouent automatiquement. |
| Sauvagerie Animale* | Passif | Après annonce : 1D6 (+2 si Blocage/Blitz). 4+ → action OK. 1-3 → Plaque un coéquipier Debout adjacent (sinon Déconcentré). |
| Soif de Sang (X+)* | Passif | Après annonce : 1D6 (+1 si Blocage/Blitz). ≥ X → action OK. Échec → remplacement par Mouvement possible. À la fin de l'activation : doit mordre un coéquipier Trois-quart Sbire adjacent → Jet de Blessure (Éliminé → Commotion). Sans morsure → Turnover + Déconcentré + perd le ballon + pas de Touchdown. |
| Solitaire (X+)* | Passif | Quand utilise une Relance d'Équipe : 1D6. ≥X → OK. Sinon, relance perdue sans effet. |
| Souffle Ardent | Passif | Action Spéciale Souffle Ardent : 1D6 (-1 si cible F ≥5). 1 → lui-même Plaqué. 2-3 → rien. 4-5 → cible Mise À Terre. 6 → cible Plaquée. |
| Timmm–ber ! | Passif | Si M ≤ 2 et tente de se relever, +1 au jet par coéquipier Debout Démarqué adjacent (1 naturel échoue toujours). |
| Toujours Affamé* | Passif | Sur Lancer de Coéquipier, 1D6 avant le Test CP. 2+ → normal. 1 → tente de manger ; deuxième 1D6. 2+ → coéquipier s'échappe, Maladresse auto. 1 → mange (retiré définitivement, pas d'Apo/Régén). |
| Tronçonneuse* | Actif | Action Spéciale Attaque de Tronçonneuse : 1D6. 2+ → Jet d'Armure avec +3. 1 → Plaqué. Plaqué/Chute → Jet d'Armure +3. Utilisable lors d'Agression : +3 Jet d'Armure (avec risque de dérapage). |

---

## 10. Coups de Pouce (Inducements)

Achat avant match avec Trésorerie ou Petite Monnaie (Jeu en Ligue) ou Budget de Sélection (Jeu Égal).

| Coup de Pouce | Limite | Coût (po) | Accès |
|---------------|--------|-----------|-------|
| Prières à Nuffle | 0-3 | 10 000 | Toutes |
| Coachs Assistants à Temps Partiel | 0-5 | 20 000 | Toutes |
| Cheerleaders Intérimaires | 0-5 | 5 000 | Toutes |
| Mascotte d'Équipe | 0-1 | 25 000 | Toutes |
| Mage Météo | 0-1 | 25 000 | Toutes |
| Fûts de Blitz Premium | 0-2 | 50 000 | Toutes |
| Pots-de-vin | 0-3 (0-6 si C&C) | 100 000 (50 000 si C&C) | Toutes |
| Entraînement Supplémentaire | 0-8 | 100 000 | Toutes |
| Assistant Funéraire | 0-1 | 100 000 | Maîtres de la Non-vie |
| Médecin de la Peste | 0-1 | 100 000 | Favoris de Nurgle |
| Débutants Déchaînés | 0-1 | 150 000 | Trois-quarts à Vil Prix |
| Apothicaire Ambulant | 0-2 | 100 000 | Équipes pouvant avoir un Apothicaire |
| Chef Cuistot Halfling | 0-1 | 300 000 (100 000 Halflings) | Toutes |
| Arbitre Partial | 0-1 | Variable | Variable |
| Staff Célèbre | 0-1 | Variable | Variable |
| Joueurs Mercenaires | 0-3 | Variable | Toutes |
| Star Players | 0-2 | Variable | Variable |
| Sorcier | 0-1 | Variable | Toutes |

**Effets clés :**
- **Mascotte d'Équipe** : +1 Relance d'Équipe par mi-temps (4+ pour l'utiliser, sinon perdue).
- **Fût de Blitz Premium** : +1 aux jets de rétablissement KO par fût.
- **Pot-de-vin** (suite Expulsion) : 1D6 ; 2+ → annule l'Expulsion. 1 → perdu.
- **Entraînement Supplémentaire** : +1 Relance d'Équipe par achat.
- **Chef Cuistot Halfling** : 3D6 en début de mi-temps ; chaque 4+ → +1 Relance pour soi, -1 pour l'adversaire.
- **Joueurs Mercenaires** : Joueur normal +30 000 po, gagne Solitaire (4+), peut prendre 1 Compétence Principale pour 50 000 po.

### 10.1 Tableau des Prières à Nuffle (D16)

| D16 | Nom | Effet |
|-----|-----|-------|
| 1 | Trappe Traîtresse | Joueur entrant sur une Trappe : 1D6 ; 1 → Blessure comme Poussé dans Public. |
| 2 | Pote avec l'Arbitre | Contester la Décision : 5-6 traité comme « présenté comme ça… » |
| 3 | Stylet | 1 joueur (au hasard) gagne Trait Poignard pour le match. |
| 4 | Homme de Fer | 1 joueur choisi : +1 AR (max 11+) pour le match. |
| 5 | Gants Cloutés | 1 joueur choisi : Châtaigne pour le match. |
| 6 | Mauvaises Habitudes | D3 joueurs adverses au hasard : Solitaire (2+) pour le match. |
| 7 | Crampons Graisseux | 1 adverse au hasard : -1 M (min 1) pour le match. |
| 8 | Bénédiction de Nuffle | 1 joueur (hasard) : Pro pour le match. |
| 9 | Des Taupes sous le Terrain | Adversaires : -1 au jet pour Foncer. |
| 10 | Passe Parfaite | Réussites des joueurs de l'équipe : 2 PSP au lieu de 1. |
| 11 | Réception Étourdissante | Toute Réception sur Passe réussie : +1 PSP. |
| 12 | Interaction avec les Fans | Adversaire Éliminé via Poussé dans Public : +2 PSP au pousseur. |
| 13 | Frénésie d'Agression | Élimination par Agression : +2 PSP. |
| 14 | Lancer de Pierre | 1/match, au début d'un tour : 1 adverse (hasard) ; 4+ → Plaqué. |
| 15 | Sous Surveillance | Adverse qui pénètre armure sur Agression : Expulsé auto (même sans double). |
| 16 | Entraînement Intensif | 1 joueur (hasard) gagne 1 Compétence Principale au choix pour le match. |

---

## 11. Jeu en Ligue

### 11.1 Points de Star Player (PSP)

| Action | PSP |
|--------|-----|
| Réussite (Passe Précise complète avec Réception sans Rebond) | 1 |
| Lancer de Coéquipier (Lancer Superbe + atterrissage réussi) — lanceur | 1 |
| Lancer de Coéquipier — coéquipier lancé qui atterrit sur ses pieds | 1 |
| Interception | 2 |
| Élimination infligée (via Blocage) | 2 |
| Touchdown | 3 |
| Joueur du Match (JDM) | 4 |

**Modificateurs équipes** :
- `Bagarreurs Brutaux` : Élimination = 3 PSP / Touchdown = 2 PSP.

**JDM** : chaque coach nomme 6 joueurs ayant joué, 1D6 désigne le JDM. Pas pour Star Players. Si Concédé : adversaire récompense 2 JDM.

### 11.2 Tableau des Améliorations (PSP requis)

| Niveau | Au hasard P | Choisir P | Choisir S | Caractéristique |
|--------|-------------|-----------|-----------|-----------------|
| 1 – Expérimenté | 3 | 6 | 10 | 14 |
| 2 – Vétéran | 4 | 8 | 12 | 16 |
| 3 – Future Star | 6 | 12 | 16 | 20 |
| 4 – Star | 8 | 16 | 20 | 24 |
| 5 – Superstar | 10 | 20 | 24 | 28 |
| 6 – Légende | 15 | 30 | 34 | 38 |

### 11.3 Tirage aléatoire de Compétences (2 × D6)

| 1er D6 | 2e D6 | Agilité | Force | Générale | Mutations | Passe | Scélérate |
|--------|-------|---------|-------|----------|-----------|-------|-----------|
| 1-3 | 1 | Réception | Clé de Bras | Blocage (E) | Main Démesurée | Précision | Joueur Déloyal |
| 1-3 | 2 | Réception Plongeante | Bagarreur | Intrépide | Griffes | Canonnier | Fourchette |
| 1-3 | 3 | Tacle Plongeant | Esquive en Force | Parade | Présence Perturbante | Perce–Nuages | Fumblerooski |
| 1-3 | 4 | Esquive (E) | Dans le Mille | Frénésie | Bras Supplémentaires | Délestage | Vol Fatal |
| 1-3 | 5 | Défenseur | Projection | Frappe Précise | Répulsion | Transmission dans la course | Agresseur Solitaire |
| 1-3 | 6 | Frappe-et-Court | Garde (E) | Pro | Cornes | Passe Désespérée | Marteau-pilon |
| 4-6 | 1 | Rétablissement | Juggernaut | Appuis Sûrs | Peau de Fer | Chef | Coup de Crampons |
| 4-6 | 2 | Saut | Châtaigne (E) | Arracher le Ballon | Grande Gueule | Nerfs d'Acier | Agression Éclair |
| 4-6 | 3 | Libération Contrôlée | Blocage Multiple | Prise Sûre | Queue Préhensile | Sur le Ballon | Saboteur |
| 4-6 | 4 | Glissade Contrôlée | Stabilité | Tacle | Tentacules | Passe | Poursuite |
| 4-6 | 5 | Sprint | Bras Musclé | Provocation | Deux Têtes | Dégagement | Sournois |
| 4-6 | 6 | Équilibre | Crâne Épais | Lutte | Très Longues Jambes | Passe Assurée | Innovateur Violent |

### 11.4 Tableau d'Amélioration de Caractéristique (1D8)

| D8 | Résultat |
|----|----------|
| 1 | +1 AR |
| 2 | +1 AR ou +1 CP |
| 3-4 | +1 AR, +1 M ou +1 CP |
| 5 | +1 M ou +1 CP |
| 6 | +1 AG ou +1 M |
| 7 | +1 AG ou +1 F |
| 8 | +1 au choix |

Refus possible → choisir Compétence Principale/Secondaire à la place (PSP dépensés perdus).

### 11.5 Hausse de Valeur

| Type d'Amélioration | Hausse |
|---------------------|--------|
| Compétence Principale | +20 000 po |
| Compétence Secondaire | +40 000 po |
| Compétence d'Élite | +10 000 po supplémentaires |
| +1 AR | +10 000 po |
| +1 M | +20 000 po |
| +1 CP | +20 000 po |
| +1 AG | +30 000 po |
| +1 F | +60 000 po |

### 11.6 Gains après match

**Formule :**
```
Affluence des Fans = FP équipe A + FP équipe B
Gains équipe = ((Affluence / 2) + TDs marqués + (1 si pas de Temporiser)) × 10 000 po
```

> ⚠️ Le bonus "+1 si pas de Temporiser" n'apparaît qu'au texte principal (p. 56). La feuille de référence (p. 116) simplifie en `((ΣFP)/2 + TD) × 10 000`, sans mentionner le bonus.

### 11.7 Mise à jour des Fans Dévoués
- Victoire : 1D6 ; si ≥ FD actuel, FD +1 (max 7).
- Défaite : 1D6 ; si < FD, FD -1 (min 1).
- Nul : inchangé.
- Concédé : -D3.

### 11.8 Tableau des Erreurs Coûteuses

À chaque fin de match, si Trésorerie ≥ 100 000 po :

| 1D6 | 100k-195k | 200k-295k | 300k-395k | 400k-495k | 500k-595k | ≥600k |
|-----|-----------|-----------|-----------|-----------|-----------|-------|
| 1 | Mineur | Mineur | Majeur | Majeur | Catastrophe | Catastrophe |
| 2 | Crise Évitée | Mineur | Mineur | Majeur | Majeur | Catastrophe |
| 3 | Crise Évitée | Crise Évitée | Mineur | Mineur | Majeur | Majeur |
| 4 | Crise Évitée | Crise Évitée | Crise Évitée | Mineur | Mineur | Majeur |
| 5 | Crise Évitée | Crise Évitée | Crise Évitée | Crise Évitée | Mineur | Mineur |
| 6 | Crise Évitée | Crise Évitée | Crise Évitée | Crise Évitée | Crise Évitée | Mineur |

- **Crise Évitée** : rien.
- **Incident Mineur** : -D3 × 10 000 po.
- **Incident Majeur** : Trésorerie ÷ 2, arrondi aux 5 000 inférieurs.
- **Catastrophe** : Trésorerie réduite à 2D6 × 10 000 po.

### 11.9 Valeur d'Équipe (VE) et VEA

- **VE** = ΣValeur Actuelle joueurs + Staff + Relances.
- **VEA** = VE - Valeur Actuelle des joueurs qui ratent le prochain match. Les Journaliers comptent dedans.
- `Trois-quarts à Vil Prix` : coût des Trois-quarts = 0 dans VEA (hausse de valeur conservée).

### 11.10 Petite Monnaie (Coups de Pouce)
- Équipe avec VEA plus haute dépense de sa Trésorerie pour acheter des Coups de Pouce.
- Équipe avec VEA plus basse reçoit : (différence de VEA + or dépensé par l'autre) en Petite Monnaie. Peut ajouter ≤50 000 de sa Trésorerie. Non dépensée → perdue.

### 11.11 Staff de Banc de Touche

| Staff | Limite | Coût |
|-------|--------|------|
| Coach Assistant | 0-6 | 10 000 |
| Cheerleader | 0-6 | 10 000 |
| Apothicaire | 0-1 | 50 000 |
| Fans Dévoués (à la création) | 1-3 (max ; +6 au cours de la ligue jusqu'à 7) | 5 000 / niveau |
| Relances d'Équipe | 0-8 | Variable (double prix en cours de ligue) |

### 11.12 Points de Ligue

- Victoire : 3
- Nul : 1
- Défaite : 0
- Bonus optionnels : +1 si ≥3 TDs / +1 si 0 TD concédé / +1 si ≥3 Éliminations infligées (qui rapportent PSP).

### 11.13 Cycle saisonnier

`Saison Normale` → `Play-offs` → `Pause Hors Saison`

Hors saison :
- Repos & Récréation : joueurs RPM se rétablissent. BP : 1D6 (+1 si Apothicaire) ; 4+ → guérison. Haine (X) acquise : 1D6, 4+ → perdue. Retraite Temporaire : 1D6 (+1 si Apo) ; 4+ → guérison.
- Levée de Fonds : 1 000 000 + (20 000 × matchs joués) + (20 000 × victoires) + (10 000 × nuls). Plafond conseillé : 1 300 000.
- Ré-enrôlement : copie de profil ; coût = Valeur Actuelle + 20 000 par Saison passée (frais d'agent).

### 11.14 Trophées
- 3e place : 30 000 po
- 2e place : 60 000 po
- 1re place : 100 000 po + Trophée de Ligue (1 Relance gratuite tant que détenu, comptée dans VE).

---

## 12. Équipes (29 races)

Légende : `M F AG CP AR` | Compétences/Traits de départ | Catégories de compétences (P=Principale ; S=Secondaire).

> ⚠️ Dans le texte brut source, les colonnes Principales/Secondaires sont souvent mal alignées par `pdftotext`. Les valeurs ci-dessous sont reconstituées au mieux. Vérifier dans le PDF original en cas de litige.

### 12.1 Alliance du Vieux Monde
- **Ligue** : Classique du Vieux Monde
- **Catégorie** : 1
- **Relances** : 70 000 / **Apothicaire** : Oui
- **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences/Traits | P | S |
|-----|-------|------|---|---|----|----|----|---|----|----|
| 0-16 | Trois-quart Humain | 50k | 6 | 3 | 3+ | 4+ | 9+ | – | G | A,F |
| 0-3 | Aspirant Halfling (Trois-quart Halfling) | 30k | 5 | 2 | 3+ | 4+ | 7+ | Minus, Esquive, Poids Plume | A | G,F |
| 0-1 | Receveur Humain | 75k | 8 | 3 | 3+ | 4+ | 8+ | Esquive, Réception | G,A | P,F |
| 0-3 | Trois-quart Nain | 70k | 4 | 3 | 4+ | 5+ | 10+ | Défenseur, Blocage, Crâne Épais | S,G | F |
| 0-1 | Lanceur Humain | 75k | 6 | 3 | 3+ | 3+ | 9+ | Passe, Prise Sûre | G,P | F,A |
| 0-1 | Coureur Nain | 80k | 6 | 3 | 3+ | 4+ | 9+ | Crâne Épais, Prise Sûre, Sprint | G,P | F,A |
| 0-1 | Blitzer Humain | 85k | 7 | 3 | 3+ | 4+ | 9+ | Blocage, Tacle | G,F | A |
| 0-1 | Blitzer Nain | 100k | 5 | 3 | 4+ | 4+ | 10+ | Crâne Épais, Blocage, Tacle, Tacle Plongeant | G,F | P |
| 0-1 | Tueur de Troll Nain | 95k | 5 | 3 | 4+ | 5+ | 9+ | Crâne Épais, Blocage, Intrépide, Frénésie, Haine (Troll) | G,F | A |
| 0-1 | Gros Bras Ogre | 140k | 5 | 5 | 4+ | 5+ | 10+ | Cerveau Lent, Crâne Épais, Solitaire (3+), Châtaigne, Lancer de Coéquipier | F | G,A |
| 0-1 | Gros Bras Homme-arbre | 120k | 2 | 6 | 5+ | 5+ | 11+ | Châtaigne, Stabilité, Bras Musclé, Prendre Racine, Crâne Épais, Lancer de Coéquipier, Timmm–ber ! | F | A,G,P |

### 12.2 Amazones
- **Ligue** : Super-ligue de Lustrie / **Catégorie** : 1
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Guerrière Aigle (Trois-quart) | 50k | 6 | 3 | 3+ | 4+ | 8+ | Esquive | G | F,A |
| 0-2 | Guerrière Python (Lanceuse) | 80k | 6 | 3 | 3+ | 3+ | 8+ | Esquive, Passe, Sur le Ballon, Passe Assurée | G,P | F,A |
| 0-2 | Guerrière Piranha (Blitzer) | 90k | 7 | 3 | 3+ | 4+ | 8+ | Esquive, Rétablissement, Frappe-et-Court | G,A | F |
| 0-2 | Guerrière Jaguar (Bloqueuse) | 110k | 6 | 4 | 3+ | 4+ | 9+ | Esquive, Défenseur | G,F | A |

### 12.3 Bas-fonds
- **Ligue** : Défi des Bas-fonds / **Catégorie** : 1
- **Relances** : 70 000 / **Apothicaire** : Oui / **Règles spéciales** : Chantage & Corruption

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Gobelin | 40k | 6 | 2 | 3+ | 4+ | 8+ | Esquive, Poids Plume, Minus | A,S,M | G,P,F |
| 0-6 | Trois-quart Snotling | 15k | 5 | 1 | 3+ | 4+ | 6+ | Esquive, Insignifiant, Poids Plume, Minus, Glissade Contrôlée, Microbe | A,S,M | G |
| 0-3 | Rat des Clans (Skaven Trois-quart) | 50k | 7 | 3 | 3+ | 4+ | 8+ | Animosité (Gobelins) | S,G,M | A,F |
| 0-1 | Lanceur Skaven | 80k | 7 | 3 | 3+ | 2+ | 8+ | Animosité (Gobelins), Passe, Prise Sûre | G,M,P | A,S,F |
| 0-1 | Coureur d'Égouts (Skaven) | 85k | 9 | 2 | 2+ | 4+ | 8+ | Animosité (Gobelins), Esquive, Poignard | A,S,G,M | F |
| 0-1 | Blitzer Skaven | 90k | 8 | 3 | 3+ | 4+ | 9+ | Animosité (Gobelins), Blocage, Arracher le Ballon | G,M,F | A,S |
| 0-1 | Gros Bras Troll | 115k | 4 | 5 | 5+ | 5+ | 10+ | Toujours Affamé, Solitaire (4+), Gerbe de Vomi, Châtaigne, Gros Débile, Régénération, Lancer de Coéquipier | M,F | G,A,P |
| 0-1 | Gros Bras Rat Ogre (Skaven) | 150k | 6 | 5 | 4+ | 6+ | 9+ | Sauvagerie Animale, Frénésie, Solitaire (4+), Châtaigne, Queue Préhensile | M,F | G,A |

### 12.4 Bretonniens
- **Ligue** : Classique du Vieux Monde / **Catégorie** : 2
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Écuyer Bretonnien (Trois-quart) | 50k | 6 | 3 | 3+ | 4+ | 8+ | Lutte | G | A,F |
| 0-2 | Receveur Chevalier Bretonnien | 85k | 7 | 3 | 3+ | 4+ | 9+ | Intrépide, Nerfs d'Acier, Réception | A,G | F |
| 0-2 | Lanceur Chevalier Bretonnien | 80k | 6 | 3 | 3+ | 3+ | 9+ | Intrépide, Nerfs d'Acier, Passe | G,P | A,F |
| 0-2 | Chevalier du Graal (Blitzer) | 95k | 7 | 3 | 3+ | 4+ | 10+ | Blocage, Intrépide, Appuis Sûrs | G,F | A |

### 12.5 Elfes Noirs
- **Ligue** : Ligue des Royaumes Elfiques / **Catégorie** : 1
- **Relances** : 50 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Elfe | 65k | 6 | 3 | 2+ | 3+ | 9+ | – | G,A | S,F |
| 0-2 | Coureur Elfe | 80k | 7 | 3 | 2+ | 3+ | 8+ | Délestage, Dégagement | G,A,P | S,F |
| 0-2 | Assassin Elfe Spécial | 90k | 7 | 3 | 2+ | 4+ | 8+ | Poursuite, Poignard, Frappe-et-Court | S,A | F,G |
| 0-2 | Blitzer Elfe | 105k | 7 | 3 | 2+ | 3+ | 9+ | Blocage | G,A | S,F,P |
| 0-2 | Furie Elfe Spéciale | 110k | 7 | 3 | 2+ | 4+ | 8+ | Esquive, Frénésie, Rétablissement | G,A | F,S |

### 12.6 Elfes Sylvains
- **Ligue** : Ligue des Royaumes Elfiques / **Catégorie** : 1
- **Relances** : 50 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Elfe | 65k | 7 | 3 | 2+ | 3+ | 8+ | – | G,A | F |
| 0-2 | Lanceur Elfe | 85k | 7 | 3 | 2+ | 2+ | 8+ | Passe, Libération Contrôlée | G,A,P | F |
| 0-2 | Receveur Elfe | 90k | 8 | 2 | 2+ | 3+ | 8+ | Réception, Esquive, Sprint | G,A | F,P |
| 0-2 | Danseur de Guerre (Blitzer) | 130k | 8 | 3 | 2+ | 3+ | 8+ | Blocage, Esquive, Saut | G,A | F,P |
| 0-1 | Gros Bras Homme-arbre | 120k | 2 | 6 | 5+ | 5+ | 11+ | Solitaire (4+), Châtaigne, Stabilité, Bras Musclé, Prendre Racine, Crâne Épais, Lancer de Coéquipier | F | A,G,P |

### 12.7 Élus du Chaos
- **Ligue** : Clash du Chaos / **Catégorie** : 3
- **Relances** : 50 000 / **Apothicaire** : Oui / **Règles spéciales** : Favoris de… (au choix)

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Homme-Bête | 55k | 6 | 3 | 3+ | 3+ | 9+ | Cornes, Crâne Épais | G,M | A,S,F,P |
| 0-4 | Élu du Chaos (Bloqueur) | 100k | 5 | 4 | 3+ | 5+ | 10+ | Clé de Bras | G,F,M | A,S |
| 0-1 | Gros Bras Troll | 115k | 4 | 5 | 5+ | 5+ | 10+ | Toujours Affamé, Solitaire (4+), Gerbe de Vomi, Châtaigne, Gros Débile, Régénération, Lancer de Coéquipier | F,M | G,A,P |
| 0-1 | Gros Bras Ogre | 140k | 5 | 5 | 4+ | 5+ | 10+ | Cerveau Lent, Crâne Épais, Solitaire (4+), Châtaigne, Lancer de Coéquipier | F,M | G,A |
| 0-1 | Gros Bras Minotaure | 150k | 5 | 5 | 4+ | 6+ | 9+ | Solitaire (4+), Frénésie, Cornes, Châtaigne, Crâne Épais, Fureur Débridée | F,M | G,A |

### 12.8 Gnomes
- **Ligue** : Coupe du Dé à Coudre Halfling, Ligue Sylvestre / **Catégorie** : 4
- **Relances** : 50 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Gnome | 40k | 5 | 2 | 3+ | 4+ | 7+ | Rétablissement, Poids Plume, Minus, Lutte | A | S,G,F |
| 0-2 | Renard Sylvestre (Coureur Animal) | 50k | 7 | 2 | 2+ | – | 6+ | Esquive, Mon Ballon, Glissade Contrôlée, Minus | – | A |
| 0-2 | Illusionniste Gnome Spécial | 50k | 5 | 2 | 3+ | 3+ | 7+ | Rétablissement, Minus, Farceur, Lutte | A,P | S,G |
| 0-2 | Belluaire Gnome (Bloqueur) | 55k | 5 | 2 | 3+ | 4+ | 8+ | Garde, Rétablissement, Minus, Lutte | A | S,G,F |
| 0-2 | Gros Bras Homme-arbre | 120k | 2 | 6 | 5+ | 5+ | 11+ | Châtaigne, Stabilité, Bras Musclé, Prendre Racine, Crâne Épais, Lancer de Coéquipier, Timmm–ber ! | F | A,G,P |

### 12.9 Gobelins
- **Ligue** : Bagarre des Terres Arides, Défi des Bas-Fonds / **Catégorie** : 4
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Chantage & Corruption

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Gobelin | 40k | 6 | 2 | 3+ | 4+ | 8+ | Esquive, Poids Plume, Minus | A,S | G,F,P |
| 0-1 | Cinglé (Gobelin Spécial) | 40k | 6 | 2 | 3+ | – | 8+ | Tronçonneuse, Arme Secrète, Minus, Sans Ballon | S | A,G,F |
| 0-1 | Bomba (Gobelin Spécial) | 45k | 6 | 2 | 3+ | 4+ | 8+ | Bombardier, Esquive, Arme Secrète, Minus | S,P | A,G,F |
| 0-1 | Ouligan' (Gobelin Spécial) | 60k | 6 | 2 | 3+ | 5+ | 8+ | Joueur Déloyal, Présence Perturbante, Esquive, Poids Plume, Minus, Provocation | A,S | G,F |
| 0-1 | Planeur de la Mort (Gobelin Spécial) | 65k | 6 | 2 | 3+ | 6+ | 8+ | Esquive, Poids Plume, Minus, Piqué | A | S,G,F |
| 0-1 | Fanatique (Gobelin Spécial) | 70k | 3 | 7 | 3+ | – | 8+ | Chaîne & Boulet, Sans Ballon, Arme Secrète, Minus | S,F | A,G |
| 0-1 | Échassier à Ressort | 75k | 7 | 2 | 3+ | 4+ | 8+ | Esquive, Monté sur Ressort, Minus | A | G,F,S |
| 0-2 | Gros Bras Troll Entraîné | 115k | 4 | 5 | 5+ | 5+ | 10+ | Toujours Affamé, Gros Débile, Châtaigne, Lancer de Coéquipier, Gerbe de Vomi, Régénération | F | A,G,P |

### 12.10 Halflings
- **Ligue** : Coupe du Dé à Coudre Halfling, Ligue Sylvestre / **Catégorie** : 4
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Aspirant Halfling (Trois-quart) | 30k | 5 | 2 | 3+ | 4+ | 7+ | Esquive, Poids Plume, Minus | A | S,G,F |
| 0-2 | Balaise Halfling (Bloqueur) | 50k | 5 | 2 | 3+ | 3+ | 8+ | Esquive, Parade, Minus | A,P | S,G,F |
| 0-2 | Receveur Halfling | 55k | 5 | 2 | 3+ | 4+ | 7+ | Réception, Esquive, Poids Plume, Minus, Sprint | A | S,G,F |
| 0-2 | Gros Bras Homme-arbre | 120k | 2 | 6 | 5+ | 5+ | 11+ | Châtaigne, Stabilité, Bras Musclé, Prendre Racine, Crâne Épais, Lancer de Coéquipier, Timmm–ber ! | F | A,G,P |

### 12.11 Hauts Elfes
- **Ligue** : Ligue des Royaumes Elfiques / **Catégorie** : 1
- **Relances** : 50 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Elfe | 65k | 6 | 3 | 2+ | 3+ | 9+ | – | G,A | F |
| 0-2 | Lion Blanc (Blitzer) | 110k | 7 | 3 | 2+ | 3+ | 9+ | Griffes, Lutte | G,A | F,P |
| 0-2 | Guerrier Phénix (Lanceur) | 90k | 6 | 3 | 2+ | 2+ | 9+ | Passe, Passe Assurée, Perce–Nuages | G,A,P | F |
| 0-2 | Prince Dragon (Blitzer/Coureur) | 110k | 8 | 3 | 2+ | 4+ | 9+ | Appuis Sûrs, Blocage, Mon Ballon | G,A | F |

### 12.12 Hommes-lézards
- **Ligue** : Super-ligue de Lustrie / **Catégorie** : 1
- **Relances** : 70 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Skink | 60k | 8 | 2 | 3+ | 4+ | 8+ | Esquive, Minus | A | G,S,P,F |
| 0-2 | Skink Caméléon (Lanceur) | 70k | 7 | 2 | 3+ | 3+ | 8+ | Esquive, Sur le Ballon, Poursuite, Minus | A,P | G,S,F |
| 0-6 | Bloqueur Saurus | 90k | 6 | 4 | 5+ | 6+ | 10+ | Juggernaut, Instable | G,F | A |
| 0-1 | Gros Bras Kroxigor | 140k | 6 | 5 | 5+ | 6+ | 10+ | Cerveau Lent, Solitaire (4+), Châtaigne, Crâne Épais, Queue Préhensile | F | A,G |

### 12.13 Horreurs Nécromantiques
- **Ligue** : Spot de Sylvanie / **Catégorie** : 2
- **Relances** : 70 000 / **Apothicaire** : Non / **Règles spéciales** : Maîtres de la Non-Vie

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Zombie | 40k | 4 | 3 | 4+ | 6+ | 9+ | Fourchette, Instable, Régénération | S,G | A,F |
| 0-2 | Coureur Goule | 75k | 7 | 3 | 3+ | 3+ | 8+ | Esquive, Régénération | A,G | S,P,F |
| 0-2 | Spectre (Bloqueur) | 85k | 6 | 3 | 3+ | – | 9+ | Blocage, Répulsion, Sans Ballon, Régénération, Glissade Contrôlée | G,F | A,S |
| 0-2 | Golem de Chair (Bloqueur) | 110k | 4 | 4 | 4+ | 6+ | 10+ | Régénération, Stabilité, Crâne Épais, Instable | G,F | A,S |
| 0-2 | Loup-garou (Blitzer) | 120k | 8 | 3 | 3+ | 3+ | 9+ | Griffes, Frénésie, Régénération | A,G | S,P,F |

### 12.14 Humains
- **Ligue** : Classique du Vieux Monde / **Catégorie** : 2
- **Relances** : 50 000 / **Apothicaire** : Oui / **Règles spéciales** : Capitaine

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Humain | 50k | 6 | 3 | 3+ | 4+ | 9+ | – | G | A,S,F |
| 0-3 | Aspirant Halfling (Trois-quart) | 30k | 5 | 2 | 3+ | 4+ | 7+ | Esquive, Poids Plume, Minus | A | S,G,F |
| 0-2 | Receveur Humain | 75k | 8 | 3 | 3+ | 4+ | 8+ | Réception, Esquive | G,A | S,F,P |
| 0-2 | Lanceur Humain | 75k | 6 | 3 | 3+ | 3+ | 9+ | Passe, Prise Sûre | G,P | A,S,F |
| 0-2 | Blitzer Humain | 85k | 7 | 3 | 3+ | 4+ | 9+ | Blocage, Tacle | G,F | A,S |
| 0-1 | Gros Bras Ogre | 140k | 5 | 5 | 4+ | 5+ | 10+ | Cerveau Lent, Solitaire (3+), Châtaigne, Crâne Épais, Lancer de Coéquipier | F | A,G |

### 12.15 Khorne
- **Ligue** : Clash du Chaos / **Catégorie** : 3
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Favoris de Khorne, Bagarreurs Brutaux

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Maraudeur Sanglant (Trois-quart Humain) | 50k | 6 | 3 | 3+ | 4+ | 8+ | Frénésie | G,M | A,S,F |
| 0-2 | Khorngor (Coureur Homme-Bête) | 70k | 6 | 3 | 3+ | 4+ | 9+ | Cornes, Juggernaut, Rétablissement, Crâne Épais | G,F,M | A,S,P |
| 0-4 | Rabatteur Sanglant (Bloqueur Humain) | 105k | 5 | 4 | 4+ | 6+ | 10+ | Frénésie | G,F,M | A,S |
| 0-1 | Gros Bras Rejeton Sanglant | 160k | 5 | 5 | 4+ | 6+ | 9+ | Griffes, Frénésie, Solitaire (4+), Châtaigne, Fureur Débridée | F,M | A,G |

### 12.16 Morts-Ambulants
- **Ligue** : Spot de Sylvanie / **Catégorie** : 2
- **Relances** : 70 000 / **Apothicaire** : Non / **Règles spéciales** : Maîtres de la Non-Vie

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Squelette | 40k | 5 | 3 | 4+ | 6+ | 8+ | Régénération, Crâne Épais | G | A,S,F |
| 0-16 | Trois-quart Zombie | 40k | 4 | 3 | 4+ | 6+ | 9+ | Fourchette, Régénération, Instable | S,G | A,F |
| 0-2 | Coureur Goule | 75k | 7 | 3 | 3+ | 3+ | 8+ | Esquive, Régénération | A,G | S,P,F |
| 0-2 | Blitzer Squelette | 95k | 6 | 3 | 3+ | 5+ | 9+ | Blocage, Régénération, Tacle, Crâne Épais | G,F | A,S |
| 0-2 | Momie (Bloqueur Gros Bras) | 125k | 3 | 5 | 5+ | 6+ | 10+ | Régénération, Châtaigne | F | A,G |

### 12.17 Nains
- **Ligue** : Super-ligue du Bord du Monde / **Catégorie** : 1
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Chantage & Corruption, Bagarreurs Brutaux

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Nain | 70k | 4 | 3 | 4+ | 5+ | 10+ | Blocage, Défenseur, Crâne Épais | G,S | F |
| 0-2 | Coureur Nain | 80k | 6 | 3 | 3+ | 4+ | 9+ | Prise Sûre, Crâne Épais, Sprint | G,P | F |
| 0-2 | Blitzer Nain | 100k | 5 | 3 | 4+ | 4+ | 10+ | Blocage, Tacle, Tacle Plongeant, Crâne Épais | G,F | P |
| 0-2 | Tueur de Troll Nain | 95k | 5 | 3 | 4+ | 5+ | 9+ | Blocage, Intrépide, Frénésie, Crâne Épais, Haine (Troll) | G,F | S |
| 0-1 | Gros Bras Roule-Mort | 170k | 5 | 7 | 5+ | – | 11+ | Esquive en Force, Joueur Déloyal, Juggernaut, Solitaire (4+), Châtaigne, Sans Ballon, Arme Secrète, Stabilité | S,F | G |

### 12.18 Nains du Chaos
- **Ligue** : Clash du Chaos, Bagarre des Terres Arides / **Catégorie** : 1
- **Relances** : 70 000 / **Apothicaire** : Oui / **Règles spéciales** : Favoris de Hashut

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Hobgobelin | 40k | 6 | 3 | 3+ | 4+ | 8+ | – | G | F,A |
| 0-2 | Surineur Sournois (Gobelin Spécial) | 60k | 6 | 3 | 3+ | 5+ | 8+ | Poursuite, Poignard | S,G | F,A |
| 0-4 | Bloqueur Nain du Chaos | 70k | 4 | 3 | 4+ | 6+ | 10+ | Blocage, Peau de Fer, Crâne Épais | G,F | A,S,M |
| 0-2 | Forgeflamme (Nain Spécial) | 80k | 5 | 3 | 4+ | 6+ | 10+ | Bagarreur, Souffle Ardent, Présence Perturbante, Crâne Épais | G,F | A,M |
| 0-2 | Centaure Taureau (Blitzer) | 130k | 6 | 4 | 4+ | 6+ | 10+ | Sprint, Équilibre, Crâne Épais, Instable | G,F | A,S,M |
| 0-1 | Gros Bras Minotaure | 150k | 5 | 5 | 4+ | 6+ | 9+ | Solitaire (4+), Frénésie, Cornes, Châtaigne, Crâne Épais, Fureur Débridée | M,F | G,A |

### 12.19 Noblesse Impériale
- **Ligue** : Classique du Vieux Monde / **Catégorie** : 2
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Valet Impérial (Trois-quart) | 45k | 6 | 3 | 3+ | 4+ | 8+ | Parade | G | A,F |
| 0-2 | Lanceur Impérial | 75k | 6 | 3 | 3+ | 2+ | 9+ | Passe, Transmission dans la Course, Pro | G,P | A,F |
| 0-4 | Garde du Corps (Bloqueur) | 85k | 5 | 3 | 3+ | 4+ | 9+ | Stabilité, Lutte | G,F | A |
| 0-2 | Noble Blitzer | 90k | 7 | 3 | 3+ | 4+ | 9+ | Blocage, Réception, Pro | G,A | P,F |
| 0-1 | Gros Bras Ogre | 140k | 5 | 5 | 4+ | 5+ | 10+ | Cerveau Lent, Solitaire (3+), Châtaigne, Crâne Épais, Lancer de Coéquipier | F | A,G |

### 12.20 Nordiques
- **Ligue** : Classique du Vieux Monde, Clash du Chaos / **Catégorie** : 1
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Si Clash du Chaos → Favoris de Khorne

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Pillard Nordique (Trois-quart) | 50k | 6 | 3 | 3+ | 4+ | 8+ | Blocage, Crâne Épais, Ivrogne, Instable | G | A,P,F |
| 0-2 | Sanglier de Secours (Animal Spécial) | 20k | 5 | 1 | 3+ | – | 6+ | Esquive, Sans Ballon, Minus, Microbe, Petit Remontant | – | A |
| 0-2 | Berserker Nordique (Blitzer) | 90k | 6 | 3 | 3+ | 5+ | 8+ | Blocage, Frénésie, Rétablissement | G,F | A,P |
| 0-2 | Valkyrie (Receveur/Lanceur) | 95k | 7 | 3 | 3+ | 3+ | 8+ | Réception, Intrépide, Passe, Arracher le Ballon | A,G,P | F |
| 0-2 | Ulfwerener (Bloqueur) | 105k | 6 | 4 | 4+ | 6+ | 9+ | Frénésie, Instable | G,F | A |
| 0-1 | Gros Bras Yéti | 140k | 5 | 5 | 4+ | 6+ | 9+ | Solitaire (4+), Griffes, Présence Perturbante, Frénésie, Fureur Débridée | F | G,A |

### 12.21 Nurgle
- **Ligue** : Clash du Chaos / **Catégorie** : 3
- **Relances** : 60 000 / **Apothicaire** : Non / **Règles spéciales** : Favoris de Nurgle, Bagarreurs Brutaux

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Putrescent | 40k | 5 | 3 | 4+ | 6+ | 9+ | Décomposition, Contagieux | S,G,M | A,F |
| 0-2 | Pestigor (Coureur Homme-Bête) | 70k | 6 | 3 | 3+ | 4+ | 9+ | Cornes, Contagieux, Régénération, Crâne Épais, Appuis Sûrs | G,M,F | A,P,S |
| 0-4 | Boursouflé (Bloqueur) | 110k | 4 | 4 | 4+ | 6+ | 10+ | Présence Perturbante, Répulsion, Contagieux, Régénération, Instable, Stabilité | G,M,F | A,S |
| 0-1 | Gros Bras Rejeton Putride | 140k | 4 | 5 | 5+ | 6+ | 10+ | Solitaire (4+), Répulsion, Présence Perturbante, Châtaigne, Contagieux, Gros Débile, Régénération, Tentacules, Petit Remontant | F | G,S,M |

### 12.22 Ogres
- **Ligue** : Bagarre des Terres Arides, Classique du Vieux Monde / **Catégorie** : 4
- **Relances** : 70 000 / **Apothicaire** : Oui / **Règles spéciales** : Favoris de Nurgle, Bagarreurs Brutaux, Trois-quarts à Vil Prix

> ⚠️ "Favoris de Nurgle" listé pour les Ogres semble incohérent (devrait peut-être ne pas s'appliquer ici) — à vérifier dans le PDF source.

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Gnoblar | 15k | 5 | 1 | 3+ | 4+ | 6+ | Esquive, Poids Plume, Minus, Glissade Contrôlée, Microbe | A,S | G |
| 0-5 | Bloqueur Ogre (Gros Bras) | 140k | 5 | 5 | 4+ | 5+ | 10+ | Cerveau Lent, Châtaigne, Crâne Épais, Lancer de Coéquipier | F | A,S,G,P |
| 0-1 | Botteur Ogre (Lanceur Gros Bras) | 145k | 5 | 5 | 4+ | 4+ | 10+ | Cerveau Lent, Châtaigne, Crâne Épais, Botter de Coéquipier | P,F | A,S,G |

### 12.23 Orques
- **Ligue** : Bagarre des Terres Arides / **Catégorie** : 2
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Bagarreurs Brutaux, Capitaine

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Orque | 50k | 5 | 3 | 3+ | 4+ | 10+ | – | G,F | A,S |
| 0-4 | Trois-quart Gobelin | 40k | 6 | 2 | 3+ | 4+ | 8+ | Esquive, Poids Plume, Minus | A,S | G,F,P |
| 0-2 | Lanceur Orque | 75k | 6 | 3 | 3+ | 3+ | 9+ | Passe, Prise Sûre | G,P | A,S,F |
| 0-2 | Blitzer Orque | 85k | 6 | 3 | 3+ | 4+ | 10+ | Blocage, Esquive en Force | G,F | A,S |
| 0-2 | Bloqueur Kosto | 95k | 5 | 4 | 4+ | 6+ | 10+ | Châtaigne, Provocation, Crâne Épais, Instable | G,F | A,S |
| 0-1 | Gros Bras Troll | 115k | 4 | 5 | 5+ | 5+ | 10+ | Toujours Affamé, Solitaire (4+), Gerbe de Vomi, Châtaigne, Gros Débile, Régénération, Lancer de Coéquipier | F | A,G,P |

### 12.24 Orques Noirs
- **Ligue** : Bagarre des Terres Arides / **Catégorie** : 3
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Chantage & Corruption, Bagarreurs Brutaux

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Malabar Gobelin (Trois-quart) | 45k | 6 | 2 | 3+ | 4+ | 8+ | Esquive, Poids Plume, Minus, Crâne Épais | A,S | G,P,F |
| 0-6 | Orque Noir (Bloqueur) | 90k | 4 | 4 | 4+ | 5+ | 10+ | Bagarreur, Projection | G,F | A,S |
| 0-1 | Gros Bras Troll Entraîné | 115k | 4 | 5 | 5+ | 5+ | 10+ | Toujours Affamé, Gros Débile, Châtaigne, Lancer de Coéquipier, Gerbe de Vomi, Régénération | F | A,G,P |

### 12.25 Renégats du Chaos
- **Ligue** : Clash du Chaos / **Catégorie** : 3
- **Relances** : 70 000 / **Apothicaire** : Oui / **Règles spéciales** : Favoris de… (au choix : Chaos Universel, Khorne, Nurgle, Slaanesh, Tzeentch)

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Humain Renégat (Trois-quart) | 50k | 6 | 3 | 3+ | 4+ | 9+ | Animosité (tous) | S,G,M | A,F |
| 0-1 | Gobelin Renégat (Trois-quart) | 40k | 6 | 2 | 3+ | 4+ | 8+ | Animosité (tous), Esquive, Minus, Poids Plume | A,S,M | G,P |
| 0-1 | Orque Renégat (Trois-quart) | 50k | 5 | 3 | 3+ | 4+ | 10+ | Animosité (tous) | S,G,M | A,F |
| 0-1 | Skaven Renégat (Trois-quart) | 50k | 7 | 3 | 3+ | 4+ | 8+ | Animosité (tous) | G,S,M | A,F |
| 0-1 | Elfe Noir Renégat (Trois-quart) | 65k | 6 | 3 | 2+ | 3+ | 9+ | Animosité (tous) | S,G,A,M | F |
| 0-1 | Lanceur Humain Renégat | 75k | 6 | 3 | 3+ | 3+ | 9+ | Animosité (tous), Passe, Prise Sûre | S,G,M,P | A,F |
| 0-1 | Gros Bras Troll Renégat | 115k | 4 | 5 | 5+ | 5+ | 10+ | Toujours Affamé, Solitaire (4+), Gerbe de Vomi, Châtaigne, Gros Débile, Régénération, Lancer de Coéquipier | F | G,A,M,P |
| 0-1 | Gros Bras Ogre Renégat | 140k | 5 | 5 | 4+ | 5+ | 10+ | Cerveau Lent, Crâne Épais, Solitaire (4+), Châtaigne, Lancer de Coéquipier | F | G,A,M |
| 0-1 | Gros Bras Minotaure Renégat | 150k | 5 | 5 | 4+ | 6+ | 9+ | Solitaire (4+), Frénésie, Cornes, Châtaigne, Crâne Épais, Fureur Débridée | F | G,A,M |
| 0-1 | Gros Bras Rat Ogre Renégat (Skaven) | 150k | 6 | 5 | 4+ | 6+ | 9+ | Sauvagerie Animale, Frénésie, Solitaire (4+), Châtaigne, Queue Préhensile | F | G,A,M |

> Maximum 3 Gros Bras dans une équipe Renégats.

### 12.26 Rois des Tombes
- **Ligue** : Spot de Sylvanie / **Catégorie** : 2
- **Relances** : 60 000 / **Apothicaire** : Non / **Règles spéciales** : Maîtres de la Non-Vie

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Squelette | 40k | 5 | 3 | 4+ | 6+ | 8+ | Régénération, Crâne Épais | G | A,S,F |
| 0-2 | Lanceur Squelette | 65k | 6 | 3 | 4+ | 3+ | 9+ | Passe, Régénération, Prise Sûre, Crâne Épais | G,P | A,S,F |
| 0-2 | Blitzer Squelette | 85k | 6 | 3 | 4+ | 5+ | 9+ | Blocage, Régénération, Crâne Épais | G,F | A,S |
| 0-4 | Gardien des Tombes (Gros Bras) | 115k | 4 | 5 | 5+ | 6+ | 10+ | Bagarreur, Décomposition, Régénération | F | G,A |

### 12.27 Skavens
- **Ligue** : Défi des Bas-Fonds / **Catégorie** : 2
- **Relances** : 50 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Rat des Clans (Trois-quart) | 50k | 7 | 3 | 3+ | 4+ | 8+ | – | G | S,A,M,F |
| 0-2 | Lanceur Skaven | 80k | 7 | 3 | 3+ | 2+ | 8+ | Passe, Prise Sûre | G,P | A,S,M,F |
| 0-2 | Coureur d'Égouts | 85k | 9 | 2 | 2+ | 4+ | 8+ | Esquive, Poignard | A,S,G | F,M |
| 0-2 | Blitzer Skaven | 90k | 8 | 3 | 3+ | 4+ | 9+ | Blocage, Arracher le Ballon | G,F | A,M,S |
| 0-1 | Gros Bras Rat Ogre | 150k | 6 | 5 | 4+ | 6+ | 9+ | Sauvagerie Animale, Frénésie, Solitaire (4+), Châtaigne, Queue Préhensile | F | A,G,M |

### 12.28 Snotlings
- **Ligue** : Défi des Bas-Fonds / **Catégorie** : 4
- **Relances** : 70 000 / **Apothicaire** : Oui / **Règles spéciales** : Chantage & Corruption, Trois-quarts à Vil Prix, Déferlement

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Snotling | 15k | 5 | 1 | 3+ | 4+ | 6+ | Esquive, Poids Plume, Minus, Glissade Contrôlée, Microbe, Insignifiant | A,S | G |
| 0-2 | R'bondisseur (Snotling Spécial) | 20k | 6 | 1 | 3+ | 4+ | 6+ | Esquive, Monté sur Ressort, Minus, Poids Plume, Glissade Contrôlée | A,S | G |
| 0-2 | Échassier (Coureur Snotling) | 20k | 6 | 1 | 3+ | 4+ | 6+ | Esquive, Poids Plume, Minus, Glissade Contrôlée, Sprint | A,S | G |
| 0-2 | Lance-Champi (Snotling Spécial) | 30k | 5 | 1 | 3+ | 4+ | 6+ | Bombardier, Esquive, Arme Secrète, Minus, Glissade Contrôlée, Poids Plume, Microbe | A,P,S | G |
| 0-2 | Chariot à Pompe (Gros Bras Spécial) | 100k | 5 | 5 | 5+ | 6+ | 9+ | Joueur Déloyal, Gros Débile, Juggernaut, Châtaigne, Stabilité | S,F | A,G |
| 0-2 | Gros Bras Troll Entraîné | 115k | 4 | 5 | 5+ | 5+ | 10+ | Toujours Affamé, Gros Débile, Châtaigne, Lancer de Coéquipier, Gerbe de Vomi, Régénération | F | A,G,P |

### 12.29 Union Elfique
- **Ligue** : Ligue des Royaumes Elfiques / **Catégorie** : 2
- **Relances** : 50 000 / **Apothicaire** : Oui / **Règles spéciales** : Aucune

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Elfe | 65k | 6 | 3 | 2+ | 3+ | 8+ | Fumblerooski | G,A | F |
| 0-2 | Lanceur Elfe | 75k | 6 | 3 | 2+ | 2+ | 8+ | Passe, Passe Désespérée | G,A,P | F |
| 0-2 | Receveur Elfe | 100k | 8 | 3 | 2+ | 4+ | 8+ | Réception, Nerfs d'Acier, Réception Plongeante | G,A | F |
| 0-2 | Blitzer Elfe | 115k | 7 | 3 | 2+ | 3+ | 9+ | Blocage, Glissade Contrôlée | G,A | F,P |

### 12.30 Vampires
- **Ligue** : Spot de Sylvanie / **Catégorie** : 2
- **Relances** : 60 000 / **Apothicaire** : Oui / **Règles spéciales** : Maîtres de la Non-Vie

| Qté | Poste | Coût | M | F | AG | CP | AR | Compétences | P | S |
|-----|-------|------|---|---|----|----|----|---|---|---|
| 0-16 | Trois-quart Sbire | 40k | 6 | 3 | 3+ | 4+ | 8+ | – | G | A,F |
| 0-2 | Coureur Vampire | 100k | 8 | 3 | 2+ | 3+ | 8+ | Regard Hypnotique, Régénération, Soif de Sang (2+) | A,G | F,P |
| 0-2 | Lanceur Vampire | 110k | 6 | 4 | 2+ | 2+ | 9+ | Passe, Regard Hypnotique, Régénération, Soif de Sang (2+) | A,G,P | F |
| 0-2 | Blitzer Vampire | 110k | 6 | 4 | 2+ | 4+ | 9+ | Juggernaut, Regard Hypnotique, Régénération, Soif de Sang (3+) | A,G,F | – |
| 0-1 | Gros Bras Vargheist | 150k | 5 | 5 | 4+ | 6+ | 10+ | Frénésie, Griffes, Régénération, Soif de Sang (3+), Solitaire (4+) | F | A,G |

---

## 13. Stars Players

> Liste condensée des Star Players. Pour chaque : `M F AG CP AR`, Compétences/Traits, Coût, Équipes éligibles, Règle spéciale unique. Les Méga-stars sont marquées **MS**.

| Nom | Pos. | M | F | AG | CP | AR | Coût | Équipes | Compétences principales |
|-----|------|---|---|----|----|----|------|---------|------------------------|
| Akhorne l'Écureuil | Écureuil/Blitzer | 7 | 1 | 2+ | – | 6+ | 80k | Toutes | Griffes, Intrépide, Esquive, Frénésie, Rétablissement, Solitaire (4+), Sans Ballon, Glissade Contrôlée, Minus, Microbe |
| Anqi Panqi | Bloqueur Homme-lézard | 7 | 4 | 5+ | 6+ | 10+ | 190k | Super-ligue de Lustrie | Blocage, Instable, Projection, Solitaire (4+), Stabilité |
| Barik Tirloin | Lanceur Nain | 6 | 3 | 4+ | 3+ | 9+ | 80k | Classique VM, Super-ligue BdM | Arme Secrète, Canonnier, Crâne Épais, Passe, Passe Désespérée, Prise Sûre, Solitaire (4+) |
| Bilerot Vomipeau | Bloqueur Humain | 4 | 5 | 4+ | 6+ | 10+ | 180k | Favoris de Nurgle | Agresseur Solitaire, Instable, Joueur Déloyal, Présence Perturbante, Régénération, Répulsion, Solitaire (4+) |
| Boa Kon'ssstriktor | Coureur Homme-serpent | 6 | 3 | 3+ | 4+ | 9+ | 180k | Super-ligue de Lustrie | Esquive, Glissade Contrôlée, Libération Contrôlée, Parade, Queue Préhensile, Regard Hypnotique, Solitaire (4+) |
| Bolgrot l'Écrabouilleur | Gros Bras Troll | 5 | 6 | 5+ | 4+ | 10+ | 250k | Bagarre des TA, Défi des Bas-Fonds | Châtaigne, Lancer de Coéquipier, Projection, Solitaire (4+), Régénération, Dans le Mille |
| Boomer Morvonez | Spécial Gobelin | 6 | 2 | 3+ | 3+ | 8+ | 80k | Bagarre des TA, Défi des Bas-Fonds | Arme Secrète, Bombardier, Esquive, Minus, Poids Plume, Précision, Solitaire (4+) |
| Capt'aine Karina Von Riesz | Coureur Vampire | 7 | 4 | 2+ | 3+ | 9+ | 230k | Spot de Sylvanie | Esquive, Glissade Contrôlée, Poursuite, Régénération, Rétablissement, Soif de Sang (2+), Solitaire (4+) |
| Cindy Piffretarte | Spécial Halfling | 5 | 2 | 3+ | 3+ | 7+ | 100k | Classique VM, Coupe du Dé à Coudre | Arme Secrète, Bombardier, Esquive, Minus, Précision, Solitaire (4+) |
| Comte Luthor Von Drakenborg | Bloqueur Vampire | 6 | 5 | 2+ | 3+ | 10+ | 300k | Spot de Sylvanie | Blocage, Glissade Contrôlée, Regard Hypnotique, Régénération, Solitaire (4+) |
| Eldril Fendlabise | Receveur Elfe | 8 | 3 | 2+ | 3+ | 8+ | 220k | Ligue Royaumes Elfiques | Esquive, Nerfs d'Acier, Réception, Regard Hypnotique, Solitaire (4+), Sur le Ballon |
| Érable Hautbocage | Gros Bras Homme-arbre | 3 | 5 | 5+ | 5+ | 11+ | 210k | Ligue Sylvestre | Bagarreur, Châtaigne, Crâne Épais, Projection, Solitaire (4+), Stabilité, Tentacules |
| Estelle La Veneaux | Trois-quart Humain | 6 | 3 | 3+ | 4+ | 8+ | 190k | Super-ligue de Lustrie | Esquive, Garde, Glissade Contrôlée, Présence Perturbante, Solitaire (4+) |
| Fungus le Cinglé | Spécial Gobelin | 4 | 7 | 3+ | – | 8+ | 80k | Bagarre des TA, Défi des Bas-Fonds | Arme Secrète, Chaîne & Boulet, Châtaigne, Minus, Sans Ballon, Solitaire (4+) |
| Glart Lavollée | Bloqueur Skaven | 5 | 4 | 4+ | 6+ | 9+ | 175k | Défi des Bas-Fonds | Blocage, Griffes, Juggernaut, Projection, Solitaire (4+), Stabilité |
| Gloriel Efflorescente | Lanceuse Elfe | 7 | 2 | 2+ | 2+ | 8+ | 150k | Ligue Royaumes Elfiques | Esquive, Glissade Contrôlée, Passe, Précision, Prise Sûre, Solitaire (3+) |
| Glotl Stop | Gros Bras Homme-lézard | 6 | 6 | 5+ | 6+ | 10+ | 260k | Super-ligue de Lustrie | Châtaigne, Crâne Épais, Frénésie, Queue Préhensile, Sauvagerie Animale, Solitaire (4+), Stabilité |
| Gobbo le Noir | Spécial Gobelin | 6 | 2 | 3+ | 3+ | 8+ | 210k | Bagarre des TA, Défi des Bas-Fonds | Bombardier, Esquive, Glissade Contrôlée, Minus, Poignard, Présence Perturbante, Solitaire (3+), Sournois |
| Grak (paire avec Crumbleberry) | Gros Bras Ogre | 5 | 5 | 4+ | 4+ | 10+ | 250k | Toutes | Botter de Coéquipier, Cerveau Lent, Châtaigne, Crâne Épais, Solitaire (4+) |
| Crumbleofruit (paire avec Grak) | Trois-quart Halfling | 7 | 2 | 3+ | 5+ | 7+ | (paire) | Toutes | Esquive, Minus, Poids Plume, Prise Sûre, Solitaire (4+), Vol Fatal |
| Grashnak Noirsabot | Gros Bras Minotaure | 6 | 6 | 4+ | 6+ | 9+ | 240k | Clash du Chaos | Châtaigne, Cornes, Crâne Épais, Frénésie, Fureur Débridée, Solitaire (4+) |
| Gretchen Wächter | Spécial Spectre Mort-vivant | 7 | 3 | 2+ | – | 9+ | 180k | Spot de Sylvanie | Esquive, Glissade Contrôlée, Poursuite, Présence Perturbante, Régénération, Répulsion, Rétablissement, Sans Ballon, Solitaire (4+) |
| Griff Oberwald **MS** | Blitzer Humain | 7 | 4 | 2+ | 3+ | 9+ | 300k | Classique du Vieux Monde | Blocage, Équilibre, Esquive, Parade, Sprint, Solitaire (3+) |
| Grim Croc d'Acier | Spécial Nain | 5 | 4 | 3+ | 6+ | 9+ | 190k | Super-ligue du Bord du Monde | Solitaire (4+), Blocage, Intrépide, Frénésie, Haine (Gros Bras), Blocage Multiple, Crâne Épais |
| Grombrindal | Bloqueur Nain | 5 | 3 | 3+ | 4+ | 10+ | 170k | Coupe Dé à Coudre, Classique VM, Super-ligue BdM | Blocage, Châtaigne, Crâne Épais, Équilibre, Esquive en Force, Intrépide, Solitaire (4+), Stabilité |
| Grondant Peaudemouton | Blitzer Halfling | 6 | 3 | 3+ | 5+ | 8+ | 170k | Coupe Dé à Coudre | Blocage, Cornes, Juggernaut, Solitaire (4+), Tacle, Crâne Épais |
| Guffle Gouffrapus | Bloqueur Humain | 5 | 4 | 4+ | 6+ | 10+ | 150k | Favoris de Nurgle | Contagieux, Grande Gueule, Nerfs d'Acier, Répulsion, Solitaire (4+), Sur le Ballon |
| Hakflem Pointu **MS** | Coureur Skaven | 8 | 3 | 2+ | 3+ | 8+ | 200k | Défi des Bas-Fonds | Bras Supplémentaires, Deux Têtes, Esquive, Queue Préhensile, Solitaire (4+) |
| Helmut Wulf | Spécial Humain | 6 | 3 | 3+ | – | 9+ | 140k | Classique du Vieux Monde | Arme Secrète, Pro, Sans Ballon, Solitaire (4+), Stabilité, Tronçonneuse |
| H'thark l'Implacable **MS** | Blitzer Nain | 6 | 6 | 4+ | 6+ | 10+ | 300k | Bagarre des TA, Favoris de Hashut | Blocage, Crâne Épais, Défenseur, Équilibre, Esquive en Force, Instable, Juggernaut, Solitaire (4+), Sprint |
| Ivan « l'Animal » Suaire **MS** | Blitzer Squelette MV | 6 | 4 | 4+ | 5+ | 9+ | 210k | Spot de Sylvanie | Arracher le Ballon, Blocage, Haine (Nain), Juggernaut, Présence Perturbante, Régénération, Solitaire (4+), Tacle |
| Ivar Eriksson | Blitzer Humain | 6 | 4 | 3+ | 4+ | 9+ | 215k | Classique du Vieux Monde | Blocage, Garde, Solitaire (4+), Tacle |
| Jeremiah Kool | Coureur Elfe | 8 | 3 | 1+ | 2+ | 9+ | 300k | Ligue Royaumes Elfiques | Blocage, Esquive, Délestage, Glissade Contrôlée, Nerfs d'Acier, Réception Plongeante, Passe, Solitaire (4+), Sur le Ballon |
| Jordel Flêchevive | Blitzer Elfe | 8 | 3 | 1+ | 3+ | 8+ | 280k | Ligue Royaumes Elfiques, Sylvestre | Appuis Sûrs, Blocage, Esquive, Glissade Contrôlée, Réception Plongeante, Saut, Solitaire (4+) |
| Josef Bugman | Bloqueur Nain | 5 | 3 | 3+ | 4+ | 9+ | 180k | Classique VM, Super-ligue BdM | Blocage, Crâne Épais, Ivrogne, Parade, Solitaire (4+), Tacle, Provocation |
| Karla Von Kill | Blitzer Humain | 6 | 4 | 3+ | 3+ | 9+ | 210k | Classique VM, Super-ligue de Lustrie | Blocage, Esquive, Intrépide, Rétablissement, Solitaire (4+) |
| Kiroth Œildekraken | Coureur Elfe | 7 | 3 | 2+ | 3+ | 8+ | 160k | Ligue Royaumes Elfiques | Présence Perturbante, Répulsion, Solitaire (4+), Sur le Ballon, Tacle, Tentacules |
| Kreek Arracherouille | Gros Bras/Spécial Skaven | 5 | 7 | 4+ | – | 10+ | 180k | Défi des Bas-Fonds | Arme Secrète, Chaîne & Boulet, Châtaigne, Queue Préhensile, Sans Ballon, Solitaire (4+) |
| Lord Borak le Destructeur | Bloqueur Humain | 5 | 5 | 3+ | 5+ | 10+ | 270k | Clash du Chaos | Blocage, Châtaigne, Chef, Joueur Déloyal, Solitaire (3+), Sournois, Coup de Crampons |
| Max Éclaterate | Spécial Humain | 5 | 4 | 4+ | – | 9+ | 130k | Favoris de Khorne | Arme Secrète, Sans Ballon, Solitaire (4+), Tronçonneuse |
| Morg 'n' Thorg **MS** | Gros Bras Ogre | 6 | 6 | 3+ | 4+ | 11+ | 340k | Toutes sauf Spot de Sylvanie | Solitaire (4+), Blocage, Châtaigne, Crâne Épais, Lancer de Coéquipier, Haine (Morts-vivants), Dans le Mille |
| Nobbla La Teigne | Spécial Gobelin | 6 | 2 | 3+ | – | 8+ | 120k | Bagarre des TA, Défi des Bas-Fonds | Arme Secrète, Blocage, Esquive, Minus, Saboteur, Sans Ballon, Solitaire (4+), Tronçonneuse |
| Perss' (paire avec Dribl') | Skink Spécial | 8 | 2 | 3+ | 4+ | 8+ | 230k | Super-ligue de Lustrie | Esquive, Glissade Contrôlée, Minus, Poignard, Solitaire (4+) |
| Dribl' (paire avec Perss') | Skink Spécial | 8 | 2 | 3+ | 4+ | 8+ | (paire) | Super-ligue de Lustrie | Agression Éclair, Esquive, Glissade Contrôlée, Joueur Déloyal, Minus, Solitaire (4+), Sournois |
| Puggy Haleinedebacon | Blitzer Halfling | 5 | 3 | 3+ | 3+ | 8+ | 130k | Coupe Dé à Coudre, Classique VM | Blocage, Esquive, Minus, Nerfs d'Acier, Poids Plume, Solitaire (3+) |
| Racine Dutronc | Gros Bras Homme-arbre | 2 | 7 | 5+ | 4+ | 11+ | 280k | Ligue Sylvestre | Blocage, Bras Musclé, Châtaigne, Crâne Épais, Dans le Mille, Lancer de Coéquipier, Solitaire (4+), Stabilité, Timmm–ber ! |
| Rashnak Lamedansledos | Spécial Gobelin | 7 | 3 | 3+ | 5+ | 8+ | 130k | Bagarre des TA | Glissade Contrôlée, Poignard, Poursuite, Solitaire (4+), Sournois |
| Rodney Pêchegardon | Spécial Gnome | 6 | 2 | 3+ | 4+ | 7+ | 70k | Ligue Sylvestre | Solitaire (4+), Réception, Réception Plongeante, Rétablissement, Glissade Contrôlée, Sur le Ballon, Minus, Lutte |
| Rowana Piéforestier | Bloqueur Gnome | 6 | 3 | 3+ | 4+ | 8+ | 160k | Ligue Sylvestre | Esquive, Délestage, Garde, Cornes, Rétablissement, Saut, Solitaire (4+) |
| Roxanna Onglenoirs | Spécial Elfe | 8 | 3 | 1+ | 3+ | 8+ | 270k | Ligue Royaumes Elfiques | Esquive, Frénésie, Juggernaut, Rétablissement, Saut, Solitaire (4+) |
| Scrappa Malocrâne | Spécial Gobelin | 7 | 2 | 3+ | 4+ | 8+ | 120k | Bagarre des TA, Défi des Bas-Fonds | Esquive, Équilibre, Joueur Déloyal, Minus, Monté sur Ressort, Poids Plume, Solitaire (4+), Sprint |
| Scyla Anfingrimm | Gros Bras Rejeton | 5 | 5 | 4+ | 6+ | 10+ | 200k | Favoris de Khorne | Châtaigne, Crâne Épais, Frénésie, Fureur Débridée, Griffes, Queue Préhensile, Solitaire (4+) |
| Skitter Pic-Pic | Coureur Skaven | 9 | 2 | 2+ | 4+ | 8+ | 170k | Défi des Bas-Fonds | Esquive, Solitaire (4+), Queue Préhensile, Poursuite, Poignard |
| Skrâne Demitaille | Lanceur Nain Squelette MV | 6 | 3 | 4+ | 3+ | 9+ | 150k | Spot de Sylvanie, Super-ligue BdM | Crâne Épais, Nerfs d'Acier, Passe, Précision, Prise Sûre, Régénération, Solitaire (4+) |
| Skrorg Gelfourure | Gros Bras Yéti | 5 | 5 | 4+ | 6+ | 9+ | 240k | Classique VM, Super-ligue BdM | Blocage, Châtaigne, Griffes, Juggernaut, Présence Perturbante, Solitaire (4+) |
| Lucien (paire avec Valen Swift) | Blitzer Elfe | 7 | 3 | 2+ | 3+ | 9+ | 300k | Ligue Royaumes Elfiques | Blocage, Châtaigne, Solitaire (4+), Tacle |
| Valen Swift (paire avec Lucien) | Lanceur Elfe | 7 | 3 | 2+ | 2+ | 9+ | (paire) | Ligue Royaumes Elfiques | Nerfs d'Acier, Passe, Passe Assurée, Précision, Prise Sûre, Solitaire (4+) |
| Thorsson Gueuledebière | Trois-quart Humain | 6 | 3 | 4+ | 3+ | 8+ | 170k | Classique VM, Super-ligue BdM | Blocage, Crâne Épais, Ivrogne, Solitaire (4+) |
| Varag Mâche Goule | Bloqueur Orque | 6 | 5 | 3+ | 5+ | 10+ | 260k | Bagarre des Terres Arides | Solitaire (4+), Blocage, Rétablissement, Châtaigne, Crâne Épais, Instable, Haine (Morts-vivants) |
| Vrilléclaire Eclalumineux | Spécial Fiel-Follet | 7 | 2 | 3+ | 5+ | 7+ | 110k | Ligue Sylvestre | Glissade Contrôlée, Minus, Parade, Présence Perturbante, Solitaire (4+) |
| Whilhelm Chaney | Blitzer Loup-garou MV | 8 | 4 | 3+ | 4+ | 9+ | 220k | Spot de Sylvanie | Frénésie, Griffes, Lutte, Réception, Régénération, Solitaire (4+) |
| Willow Rosebark | Blitzer Dryade | 5 | 4 | 3+ | 5+ | 9+ | 160k | Ligue Sylvestre | Crâne Épais, Glissade Contrôlée, Intrépide, Solitaire (4+) |
| Withergrasp Doubledrool | Bloqueur Homme-bête | 6 | 3 | 3+ | 4+ | 9+ | 170k | Favoris de Nurgle | Deux Têtes, Lutte, Queue Préhensile, Répulsion, Solitaire (4+), Tacle, Tentacules |
| Zug la Bête | Bloqueur Humain | 5 | 5 | 4+ | 6+ | 10+ | 220k | Classique VM, Super-ligue BdM | Blocage, Châtaigne, Instable, Solitaire (4+) |
| Zzharg le Borgne | Spécial Nain | 4 | 4 | 4+ | 3+ | 10+ | 130k | Favoris de Hashut | Arme Secrète, Canonnier, Crâne Épais, Nerfs d'Acier, Passe Désespérée, Solitaire (4+) |
| Zolcath le Zoat | Gros Bras Zoat | 5 | 5 | 4+ | 5+ | 10+ | 220k | Ligue Royaumes Elfiques, Super-ligue de Lustrie | Châtaigne, Équilibre, Juggernaut, Présence Perturbante, Queue Préhensile, Régénération, Solitaire (4+) |

**Méga-stars** (Jeu Égal, 4 PC au lieu de 2) : Griff Oberwald, Hakflem Pointu, H'thark l'Implacable, Ivan « l'Animal » Suaire, Morg 'n' Thorg.

---

## 14. Annexes / Tableaux rapides

### 14.1 Règles spéciales d'équipe

| Règle | Effet |
|-------|-------|
| **Bagarreurs Brutaux** | Élimination = 3 PSP, Touchdown = 2 PSP. |
| **Chantage & Corruption** | 1/match, peut relancer un 1 sur Contester la Décision. Pot-de-vin moitié prix (0-6). |
| **Favoris de…** | Donne accès à certains Star Players / Coups de Pouce spécifiques. Variantes : Hashut, Khorne, Nurgle, Slaanesh, Tzeentch, Chaos Universel. |
| **Trois-quarts à Vil Prix** | Coût des Trois-quarts compte comme 0 dans le calcul de VEA (hausse de valeur conservée). |
| **Maîtres de la Non-Vie** | 1/match, si un adversaire F≤4 sans Minus subit Mort, peut Relever le Mort = +1 Trois-quart en Réserves. Embauche gratuite en Après-Match. |
| **Déferlement** | En début de Phase, peut placer D3 Trois-quarts supp depuis les Réserves (>11 joueurs autorisé). |
| **Capitaine** | Désigne un joueur Capitaine (hors Gros Bras) ; gagne `Pro` gratuit. Tant qu'il est sur le terrain, sur 6 naturel lors d'une Relance d'Équipe → relance gratuite. Doit être placé en priorité. |

### 14.2 Ligues officielles

- Bagarre des Terres Arides
- Classique du Vieux Monde
- Clash du Chaos
- Ligue des Royaumes Elfiques
- Coupe Dé à Coudre Halfling
- Super-ligue de Lustrie
- Spot de Sylvanie
- Défi des Bas-fonds
- Ligue Sylvestre
- Super-ligue du Bord du Monde

### 14.3 Récapitulatif des modificateurs

#### Modificateurs d'Agilité

| Test | Modif |
|------|-------|
| Esquiver — par joueur adverse marquant la case d'arrivée | -1 |
| Bondir — par joueur adverse marquant case départ OU arrivée (la plus marquée) | -1 |
| Ramasser — par joueur adverse marquant | -1 |
| Intercepter Passe Précise | -3 |
| Intercepter Passe Imprécise | -2 |
| Intercepter — par joueur marquant l'intercepteur | -1 |
| Réception (Précise) | 0 |
| Réception après Rebond | -1 |
| Réception après Renvoi | -1 |
| Réception — par joueur marquant le récepteur | -1 |
| Atterrissage Lancer Médiocre / Maladresse | -1 |
| Atterrissage — par joueur marquant la case | -1 |

#### Modificateurs de Capacité de Passe

| Test | Modif |
|------|-------|
| Passe Rapide | 0 |
| Passe Courte | -1 |
| Passe Longue | -2 |
| Longue Bombe | -3 |
| Par joueur marquant le passeur | -1 |
| Lancer Rapide | 0 |
| Lancer Court | -1 |
| Lancer — par joueur marquant lanceur | -1 |

### 14.4 Récapitulatif des dés de Blocage

| Résultat | Effet |
|----------|-------|
| Attaquant Plaqué | L'attaquant est Plaqué. |
| Les Deux Plaqués | Les deux Plaqués (`Blocage`/`Lutte` modifient). |
| Repoussé | Cible Repoussée 1 case, Poursuite possible. |
| Bousculé | `Esquive` cible → Repoussé. Sinon → Défenseur Plaqué. |
| Défenseur Plaqué | Repoussé + Plaqué. |

### 14.5 Récapitulatif des PSP

| Action | PSP standard | Bagarreurs Brutaux |
|--------|--------------|---------------------|
| Réussite | 1 | 1 |
| Lancer de Coéquipier (lanceur ou coéquipier atterri) | 1 | 1 |
| Interception | 2 | 2 |
| Élimination | 2 | 3 |
| Touchdown | 3 | 2 |
| Joueur du Match | 4 | 4 |

### 14.6 Limites Caractéristiques

| Carac | Min | Max |
|-------|-----|-----|
| M | 1 | 9 |
| F | 1 | 8 |
| AG | 6+ | 1+ |
| CP | 6+ | 1+ |
| AR | 3+ | 11+ |

- Maximum 2 améliorations de caractéristique par joueur.

### 14.7 Liste rapide des Compétences d'Élite

Compétences marquées E : **Blocage** (G), **Châtaigne** (F), **Esquive** (A), **Garde** (F).

> Hausse de valeur supplémentaire de +10 000 po lors de l'acquisition.

---

## Légende des abréviations

| Abréviation | Signification |
|-------------|---------------|
| M / F / AG / CP / AR | Caractéristiques |
| ZdT | Zone de Tacle |
| PSP | Points de Star Player |
| FD | Fans Dévoués |
| FP | Facteur de Popularité |
| VE / VEA | Valeur d'Équipe / Actuelle |
| RPM | Rate le Prochain Match |
| BP | Blessure Persistante |
| RT | Retraite Temporaire |
| JDM | Joueur du Match |
| MS | Méga-Star |
| C&C | Chantage & Corruption |
| TA / BdM / VM | Terres Arides / Bord du Monde / Vieux Monde |
| po / kpo | Pièces d'or / kilo-pièces d'or (×1 000) |
