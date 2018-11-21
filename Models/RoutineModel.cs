namespace DataBaseCompare.Models {

    public class RoutineModel : ScriptedModel {

        #region Properties

        public string Type { get; set; }

        #endregion Properties

        public override string ToString() => $"({Type}) - [{Name}]";
    }
}
