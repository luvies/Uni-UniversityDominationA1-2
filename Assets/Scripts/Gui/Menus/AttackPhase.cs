using Helpers;
using Map;
using Map.Hex;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gui.Menus
{
    public class AttackPhase : PhaseLogic
    {
        #region Unity Bindings

        // other
        public GameObject endTurnButton;

        #endregion

        #region Private Fields

        readonly Color _brightTint = new Color(1, 0.8f, 0.8f);
        readonly Color _dimmedTint = new Color(1f, 0.4f, 0.4f);
        Color _oldBrightTint;
        Color _oldDimmedTint;
        HashSet<Sector> _currentSecondaryHighlightedSectors = new HashSet<Sector>();

        #endregion

        #region Public Properties

        public override bool IsEnabled
        {
            get
            {
                return gameObject.activeInHierarchy;
            }
            set
            {
                gameObject.SetActive(value);
                sharedPanel.SetActive(value);
                if (value)
                {
                    // setup default menu state
                    endTurnButton.SetActive(true);
                    // setup tints
                    _oldBrightTint = Gc.Map.SectorMaterials.GetHighlightTint(HighlightLevel.Bright);
                    _oldDimmedTint = Gc.Map.SectorMaterials.GetHighlightTint(HighlightLevel.Dimmed);
                    SetHighlightTints(_brightTint, _dimmedTint);
                    foreach (KeyValuePair<Coord, Sector> kv in Gc.Map.Grid)
                        if (kv.Value.OccupyingUnit != null && kv.Value.OccupyingUnit.Owner.Equals(Gc.CurrentPlayer))
                            kv.Value.OccupyingUnit.HasAttacked = false;
                }
                else
                {
                    SetHighlightTints(_oldBrightTint, _oldDimmedTint);
                    DoUnitSelection(null, s => 0); // clear selection state
                }
            }
        }

        #endregion

        #region MonoBehaviour

        protected override void Update()
        {
            base.Update();

            Coord? fetchCoord = GetSectorAtScreen(Input.mousePosition);

            // build unit highlighting
            if (_currentSecondaryHighlightedSectors.Count > 0)
            {
                foreach (Sector sector in _currentSecondaryHighlightedSectors)
                    if (sector.Highlight == HighlightLevel.Bright)
                        sector.Highlight = HighlightLevel.Dimmed;
                _currentSecondaryHighlightedSectors.Clear();
            }
            if (SelectedUnit != null && fetchCoord.HasValue && Gc.Map.Grid.IsTraversable(fetchCoord.Value) && SelectedRange.Contains(Gc.Map.Grid[fetchCoord.Value]))
            {
                _currentSecondaryHighlightedSectors.Add(Gc.Map.Grid[fetchCoord.Value]);
                Gc.Map.Grid[fetchCoord.Value].Highlight = HighlightLevel.Bright;
            }
        }

        #endregion

        #region Handlers

        protected override void OnMouseLeftClick(Vector3 position)
        {
            Coord? fetchCoord = GetSectorAtScreen(position);

            if (!fetchCoord.HasValue) // if the player clicked off-screen, clear selection
                DoUnitSelection(null, s => 0);
            else if (ContainsUnit(fetchCoord.Value, true)) // if player clicked on owned unit, shift selection to that one
            {
                DoUnitSelection(fetchCoord.Value, s =>
                    s.OccupyingUnit.HasAttacked || s.OccupyingUnit.ManaAttackCost > s.OccupyingUnit.Owner.Mana ?
                        0 : s.OccupyingUnit.AttackRange);
                if (SelectedUnit != null) // if the player was able to select the unit
                { // this should pass anyway, but it's good to double check
                    // unit just selected
                }
                else
                    Debug.Log("Owned unit selection failed");
            }
            else if (SelectedUnit != null) // if player has clicked a unit before, and has now clicked on a separate space, we need to prepare to move
            {
                SelectSector(fetchCoord); // try to select the clicked sector
                // only consider an attack selection if it wasn't traversable and if an enemy was on it
                if (SelectedSector != null && SelectedSectorContainsUnit(true))
                {
                    if (SelectedRange.Contains(SelectedSector))
                        AttackUnit();
                }
            }
        }

        public void EndTurnButton_OnClick()
        {
            Gc.NextPlayer();
            Gc.Gui.CurrentMenu = MenuType.MovePhase;
        }

        #endregion

        #region Helpers

        void SetHighlightTints(Color bright, Color dimmed)
        {
            Gc.Map.SectorMaterials.SetHighlightTint(HighlightLevel.Bright, bright);
            Gc.Map.SectorMaterials.SetHighlightTint(HighlightLevel.Dimmed, dimmed);
        }

        void AttackUnit()
        {
            IUnit attacker = SelectedUnit.OccupyingUnit;
            IUnit defender = SelectedSector.OccupyingUnit;

            defender.Health -= (int)Math.Round(attacker.Attack * (1 - (defender.Defence / 100f)));
            if (defender.Health <= 0)
            {
                defender.Kill();
                SelectedSector.OccupyingUnit = null;
            }
            attacker.HasAttacked = true;
            attacker.Owner.Mana -= attacker.ManaAttackCost;
            UpdateMana();

            DoUnitSelection(null, s => 0);
        }

        #endregion
    }
}
