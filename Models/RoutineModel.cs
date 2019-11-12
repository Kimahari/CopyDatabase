namespace DataBaseCompare.Models {

    public class RoutineModel : ScriptedModel {

        #region Public Properties

        public string Type { get; set; }

        #endregion Public Properties

        #region Public Methods

        public override string ToString() {
            return $"({Type}) - [{Name}]";
        }

        #endregion Public Methods
    }
}
