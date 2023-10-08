using Cinemachine;
using UnityEngine;
using Utils;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] private Circuit circuit;
    [SerializeField] private AIDriverData aiDriverData;
    [SerializeField] private GameObject[] aiCarPrefabs;

    [SerializeField] private GameObject playerCarPrefab;
    [SerializeField] private CinemachineVirtualCamera playerCamera;

    private void Start()
    {
        var playerCar = Instantiate(playerCarPrefab, circuit.spawnPoints[0].position,
            circuit.spawnPoints[0].rotation);

        playerCamera.Follow = playerCar.transform;
        playerCamera.LookAt = playerCar.transform;

        for (int i = 1; i < circuit.spawnPoints.Length; i++)
        {
            new AICarBuilder(aiCarPrefabs[Random.Range(0, aiCarPrefabs.Length)])
                .WithCircuit(circuit)
                .WithDriverData(aiDriverData)
                .WithSpawnPoint(circuit.spawnPoints[i])
                .Build();
        }
    }

    private class AICarBuilder
    {
        private GameObject _prefab;
        private AIDriverData _data;
        private Circuit _circuit;
        private Transform _spawnPoint;

        public AICarBuilder(GameObject prefab)
        {
            _prefab = prefab;
        }

        public AICarBuilder WithDriverData(AIDriverData data)
        {
            _data = data;
            return this;
        }

        public AICarBuilder WithCircuit(Circuit circuit)
        {
            _circuit = circuit;
            return this;
        }

        public AICarBuilder WithSpawnPoint(Transform spawnPoint)
        {
            _spawnPoint = spawnPoint;
            return this;
        }

        public GameObject Build()
        {
            var instance = Object.Instantiate(_prefab, _spawnPoint.position, _spawnPoint.rotation);
            var aiInput = instance.GetOrAdd<AIInput>();
            aiInput.AddCircuit(_circuit);
            aiInput.AddDriverData(_data);
            instance.GetComponent<CarController>().SetInput(aiInput);

            return instance;
        }
    }
}