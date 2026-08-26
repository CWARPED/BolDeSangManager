// Gestion de la reconnexion du circuit Blazor Server.
//
// Contexte : Blazor Server maintient une connexion WebSocket permanente entre le
// navigateur et le serveur. Les navigateurs mobiles — Safari iOS en tête — gèlent
// le JavaScript dès que l'onglet passe en arrière-plan (appel entrant, notification,
// verrouillage de l'écran). Le keep-alive s'arrête, le serveur finit par lâcher le
// circuit, et le coach qui revient sur sa feuille de match trouve un écran mort.
//
// Le comportement par défaut de .NET 9 est déjà correct sur deux points :
//   - il retente immédiatement quand l'onglet redevient visible (pas besoin de
//     gérer 'visibilitychange' nous-mêmes pour ce cas) ;
//   - il applique un backoff exponentiel entre les tentatives.
// Ce fichier ne réimplémente donc PAS ces deux mécanismes : il complète ce que le
// défaut ne couvre pas.
//
//   1. Une planification de tentatives plus longue (~3 min) que le défaut, calée
//      sur DisconnectedCircuitRetentionPeriod côté serveur (5 min) : tant que le
//      serveur garde l'état du circuit, ça vaut le coup de retenter.
//   2. Le rechargement AUTOMATIQUE quand le serveur a définitivement lâché l'état
//      ('rejected'). Par défaut l'utilisateur reste devant un message figé qui lui
//      demande de recharger lui-même — sur mobile, beaucoup ne le font pas et
//      croient l'application cassée.
//   3. Un bouton « Réessayer » explicite quand les tentatives sont épuisées
//      ('failed'), plutôt qu'un cul-de-sac.

(() => {
    'use strict';

    // Tentatives : rapprochées au début (coupure réseau brève), puis espacées.
    // Total ≈ 3 min. Au-delà du dernier élément, .at() renvoie undefined et Blazor
    // arrête de retenter (état 'failed').
    const RETRY_SCHEDULE = [
        0, 1000, 2000, 3000, 5000, 8000, 10000,
        15000, 20000, 30000, 30000, 30000, 30000
    ];

    const MODAL_ID = 'components-reconnect-modal';

    // Empêche les rechargements en boucle si le serveur est réellement hors service.
    // La garde doit survivre AU rechargement lui-même (une variable simple serait
    // remise à zéro à chaque chargement de page, ce qui autorise une boucle infinie
    // tant que le serveur refuse le circuit). On compte donc les rechargements
    // automatiques dans sessionStorage, sur une fenêtre glissante.
    const CLE_COMPTEUR = 'bds-reconnect-reloads';
    const MAX_RECHARGEMENTS = 2;          // au-delà, on rend la main à l'utilisateur
    const FENETRE_MS = 60000;             // sur une minute glissante

    let rechargementDeclenche = false;    // garde intra-page (évite les doublons)

    function lireHistorique() {
        try {
            const brut = sessionStorage.getItem(CLE_COMPTEUR);
            const liste = brut ? JSON.parse(brut) : [];
            const limite = Date.now() - FENETRE_MS;
            return Array.isArray(liste) ? liste.filter(t => t > limite) : [];
        } catch (e) {
            return [];   // sessionStorage indisponible (navigation privée stricte)
        }
    }

    function rechargerUneSeuleFois() {
        if (rechargementDeclenche) return;

        const historique = lireHistorique();
        if (historique.length >= MAX_RECHARGEMENTS) {
            // Le serveur refuse le circuit de façon répétée : recharger encore ne
            // ferait que boucler. On bascule sur l'UI d'échec avec le bouton.
            const modale = document.getElementById(MODAL_ID);
            if (modale) {
                modale.classList.remove('components-reconnect-rejected');
                modale.classList.add('components-reconnect-failed');
            }
            return;
        }

        historique.push(Date.now());
        try {
            sessionStorage.setItem(CLE_COMPTEUR, JSON.stringify(historique));
        } catch (e) { /* pas de persistance possible : on recharge quand même */ }

        rechargementDeclenche = true;
        location.reload();
    }

    // 'rejected' = le serveur a été joint mais a refusé le circuit : son état est
    // perdu (redémarrage du conteneur, ou déconnexion plus longue que la rétention).
    // Recharger est la seule issue — autant le faire sans attendre l'utilisateur.
    // 'failed'  = plus aucune tentative en cours ; on laisse la main via le bouton.
    function surChangementEtat(classes) {
        if (classes.contains('components-reconnect-rejected')) {
            rechargerUneSeuleFois();
        } else if (classes.contains('components-reconnect-hide')) {
            // Reconnexion réussie : l'historique de rechargements ne doit pas
            // pénaliser un incident ultérieur sans rapport.
            try { sessionStorage.removeItem(CLE_COMPTEUR); } catch (e) { /* ignoré */ }
        }
    }

    function observerModale() {
        const modale = document.getElementById(MODAL_ID);
        if (!modale) return;

        const bouton = document.getElementById('reconnect-retry-button');
        if (bouton) {
            bouton.addEventListener('click', () => {
                // Blazor.reconnect() retente sur le circuit existant ; s'il a expiré
                // côté serveur, elle résout à false et il ne reste qu'à recharger.
                if (window.Blazor && typeof window.Blazor.reconnect === 'function') {
                    window.Blazor.reconnect().then(reussi => {
                        if (reussi === false) rechargerUneSeuleFois();
                    }).catch(() => rechargerUneSeuleFois());
                } else {
                    rechargerUneSeuleFois();
                }
            });
        }

        new MutationObserver(() => surChangementEtat(modale.classList))
            .observe(modale, { attributes: true, attributeFilter: ['class'] });

        // État déjà positionné avant l'installation de l'observateur.
        surChangementEtat(modale.classList);
    }

    Blazor.start({
        circuit: {
            reconnectionOptions: {
                retryIntervalMilliseconds: Array.prototype.at.bind(RETRY_SCHEDULE),
                maxRetries: RETRY_SCHEDULE.length
            },
            configureSignalR: builder => {
                // Doit rester cohérent avec ClientTimeoutInterval côté serveur
                // (Program.cs) : timeout ≥ 2 × keep-alive (15 s par défaut).
                builder.withServerTimeout(60000).withKeepAliveInterval(15000);
            }
        }
    }).then(observerModale);
})();
