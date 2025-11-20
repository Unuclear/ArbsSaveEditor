using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AseCore.ReflectionWrappers
{
    public class WeaponisedAnimalPrefab : ReflectedObject
    {
        private static readonly Dictionary<int, string> UnitDictionary;

        private readonly ReflectedObject _animalRecord;

        private string _name;
        private int _unitPrefabIndex;
        private int _team;
        private readonly Dictionary<int, int> _properties;
        private PrefabAiMode _aiMode;
        private int _detectionDistance;
        private decimal _healthFactor;
        private decimal _attackFactor;
        private decimal _sizeFactor;

        static WeaponisedAnimalPrefab()
        {
            using (var fs = File.OpenRead("Dictionaries/unit-dictionary.json"))
            {
                UnitDictionary = JsonSerializer.Deserialize<Dictionary<int, string>>(fs);
            }
        }

        public WeaponisedAnimalPrefab(object instance) : base(CheckIfInstanceIsWap(instance))
        {
            _name = (string)Get("prefabName");

            _animalRecord = GetAsReflectedObject("animalRecord");
            _unitPrefabIndex = (int)_animalRecord.Get("unitPrefabIndex");
            Position = new PrefabPosition(_animalRecord);
            Quaternion = new PrefabQuaternion(_animalRecord);
            _team = (int)_animalRecord.Get("team");

            _properties = (Dictionary<int, int>)_animalRecord.Get("props");
            _aiMode = (PrefabAiMode)_properties[0];
            _detectionDistance = _properties[1];
            _healthFactor = _properties[2] / 100m;
            _attackFactor = _properties[3] / 100m;
            _sizeFactor = _properties[4] / 100_000m;

            Attachments = new List<Attachment>();
            for (var key = 100; _properties.ContainsKey(key); key += 11)
                Attachments.Add(new Attachment(_properties, key));
        }

        public string Name
        {
            get => _name;
            set
            {
                Set("prefabName", value);
                _name = value;
            }
        }

        public int UnitPrefabIndex
        {
            get => _unitPrefabIndex;
            set
            {
                _animalRecord.Set("unitPrefabIndex", value);
                _unitPrefabIndex = value;
            }
        }

        public PrefabPosition Position { get; }
        public PrefabQuaternion Quaternion { get; }

        public int Team
        {
            get => _team;
            set
            {
                _animalRecord.Set("team", value);
                _team = value;
            }
        }

        public PrefabAiMode AiMode
        {
            get => _aiMode;
            set
            {
                _properties[0] = (int)value;
                _aiMode = value;
            }
        }

        public int DetectionDistance
        {
            get => _detectionDistance;
            set
            {
                _properties[1] = value;
                _detectionDistance = value;
            }
        }

        public decimal HealthFactor
        {
            get => _healthFactor;
            set
            {
                _properties[2] = (int)(value * 100m);
                _healthFactor = value;
            }
        }

        public decimal AttackFactor
        {
            get => _attackFactor;
            set
            {
                _properties[3] = (int)(value * 100m);
                _attackFactor = value;
            }
        }

        public decimal SizeFactor
        {
            get => _sizeFactor;
            set
            {
                _properties[4] = (int)(value * 100_000m);
                _sizeFactor = value;
            }
        }

        public List<Attachment> Attachments { get; }

        public override string ToString()
        {
            var sb = new StringBuilder("[WeaponisedAnimalPrefab] ")
                .Append(_name)
                .AppendLine(":")
                .Append("  Base unit: ")
                .Append(UnitDictionary.TryGetValue(_unitPrefabIndex, out var baseUnitName) ? baseUnitName : "Unknown")
                .Append(" (")
                .Append(_unitPrefabIndex)
                .AppendLine(")")
                .Append("  Position: ")
                .Append(Position)
                .AppendLine()
                .Append("  Rotation: ")
                .Append(Quaternion)
                .AppendLine()
                .Append("  Team: ")
                .Append(_team)
                .AppendLine()
                .Append("  AI mode: ")
                .Append(_aiMode)
                .AppendLine()
                .Append("  Detection distance: ")
                .Append(_detectionDistance)
                .AppendLine()
                .Append("  Health: ")
                .Append(_healthFactor)
                .AppendLine("x")
                .Append("  Attack: ")
                .Append(_attackFactor)
                .AppendLine("x")
                .Append("  Size: ")
                .Append(_sizeFactor)
                .Append("x");

            if (Attachments.Count == 0) return sb.ToString();

            sb.AppendLine().Append("  Attachments:");
            var indexLeftPad = Attachments.Count.DigitCount();
            for (var index = 0; index < Attachments.Count; index++)
                sb.AppendLine()
                    .Append("   [")
                    .Append((index + 1).ToString().PadLeft(indexLeftPad))
                    .Append("] ")
                    .Append(Attachments[index]);

            return sb.ToString();
        }

        internal string PropertiesAsString()
        {
            var sb = new StringBuilder()
                .AppendLine("properties: {");
            foreach (var kv in _properties)
                sb.Append("  ")
                    .Append(kv.Key)
                    .Append(": ")
                    .Append(kv.Value)
                    .AppendLine(",");
            return sb.Append("}").ToString();
        }

        private static object CheckIfInstanceIsWap(object instance)
        {
            if (instance.GetType().Name == "WeaponisedAnimalPrefab") return instance;

            throw new ArgumentException("Instance is not a WeaponisedAnimalPrefab!");
        }

        public enum PrefabAiMode
        {
            Default,
            Idle,
            Guard
        }

        public class PrefabPosition
        {
            private readonly ReflectedObject _animalRecord;

            private float _x;
            private float _y;
            private float _z;

            public PrefabPosition(ReflectedObject animalRecord)
            {
                _animalRecord = animalRecord;

                _x = (float)_animalRecord.Get("positionX");
                _y = (float)_animalRecord.Get("positionY");
                _z = (float)_animalRecord.Get("positionZ");
            }

            public float X
            {
                get => _x;
                set
                {
                    _animalRecord.Set("positionX", value);
                    _x = value;
                }
            }

            public float Y
            {
                get => _y;
                set
                {
                    _animalRecord.Set("positionY", value);
                    _y = value;
                }
            }

            public float Z
            {
                get => _z;
                set
                {
                    _animalRecord.Set("positionZ", value);
                    _z = value;
                }
            }

            public override string ToString()
            {
                return new StringBuilder("(x: ")
                    .Append(_x)
                    .Append(", y: ")
                    .Append(_y)
                    .Append(", z: ")
                    .Append(_z)
                    .Append(")")
                    .ToString();
            }
        }

        public class PrefabQuaternion
        {
            private readonly ReflectedObject _animalRecord;

            private float _x;
            private float _y;
            private float _z;
            private float _w;

            public PrefabQuaternion(ReflectedObject animalRecord)
            {
                _animalRecord = animalRecord;

                _x = (float)_animalRecord.Get("quaternionX");
                _y = (float)_animalRecord.Get("quaternionY");
                _z = (float)_animalRecord.Get("quaternionZ");
                _w = (float)_animalRecord.Get("quaternionW");
            }

            public float X
            {
                get => _x;
                set
                {
                    _animalRecord.Set("quaternionX", value);
                    _x = value;
                }
            }

            public float Y
            {
                get => _y;
                set
                {
                    _animalRecord.Set("quaternionY", value);
                    _y = value;
                }
            }

            public float Z
            {
                get => _z;
                set
                {
                    _animalRecord.Set("quaternionZ", value);
                    _z = value;
                }
            }

            public float W
            {
                get => _w;
                set
                {
                    _animalRecord.Set("quaternionW", value);
                    _w = value;
                }
            }

            public override string ToString()
            {
                return new StringBuilder("(x: ")
                    .Append(_x)
                    .Append(", y: ")
                    .Append(_y)
                    .Append(", z: ")
                    .Append(_z)
                    .Append(", w: ")
                    .Append(_w)
                    .Append(")")
                    .ToString();
            }
        }

        public class Attachment
        {
            private static readonly Dictionary<int, string> AttachmentDictionary;

            private static readonly int[] RemovedIds = { 4193, 4194, 4195 };

            private readonly Dictionary<int, int> _properties;

            private int _id;
            private decimal _sizeFactor;
            private decimal _attackFactor;

            static Attachment()
            {
                using (var fs = File.OpenRead("Dictionaries/attachment-dictionary.json"))
                {
                    AttachmentDictionary = JsonSerializer.Deserialize<Dictionary<int, string>>(fs);
                }
            }

            public Attachment(Dictionary<int, int> properties, int key)
            {
                _properties = properties;
                Key = key;

                _id = _properties[Key];
                _sizeFactor = _properties[Key + 1] / 100_000m;
                _attackFactor = _properties[Key + 2] / 100_000m;
            }

            public int Key { get; set; }

            public int Id
            {
                get => _id;
                set
                {
                    _properties[Key] = value;
                    _id = value;
                }
            }

            public bool Removed => RemovedIds.Contains(_id);

            public decimal SizeFactor
            {
                get => _sizeFactor;
                set
                {
                    _properties[Key + 1] = (int)(value * 100_000m);
                    _sizeFactor = value;
                }
            }

            public decimal AttackFactor
            {
                get => _attackFactor;
                set
                {
                    _properties[Key + 2] = (int)(value * 100_000m);
                    _attackFactor = value;
                }
            }

            public override string ToString()
            {
                if (Removed) return "Removed attachment";

                return new StringBuilder()
                    .Append(AttachmentDictionary.TryGetValue(_id, out var name) ? name : "Unknown attachment")
                    .Append(" (")
                    .Append(_id)
                    .Append("): ")
                    .Append(_sizeFactor)
                    .Append("x size, ")
                    .Append(_attackFactor)
                    .Append("x attack")
                    .ToString();
            }
        }
    }
}