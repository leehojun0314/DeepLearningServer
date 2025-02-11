using DeepLearningServer.Models;
using DeepLearningServer.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DeepLearningServer.Services;

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly string _fileDirPrefix;
    private readonly IMongoCollection<LogRecord> _logCollection;
    private readonly IMongoCollection<TrainingRecord> _trainingCollection;

    public MongoDbService(IOptions<MongoDbSettings> mongoDbSettings)
    {
        try
        {

            Console.WriteLine("Loading MongoDbSettings...");
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(mongoDbSettings.Value.ConnectionString));
            clientSettings.MaxConnectionPoolSize = 100;
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
            clientSettings.ConnectTimeout = TimeSpan.FromSeconds(30);
            clientSettings.SocketTimeout = TimeSpan.FromMinutes(5);

            var client = new MongoClient(clientSettings);
            _database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
            Console.WriteLine("Database loaded");
            _fileDirPrefix = mongoDbSettings.Value.ModelDirectory;
            _logCollection = _database.GetCollection<LogRecord>("Logs");
            _trainingCollection = _database.GetCollection<TrainingRecord>("Trainings");
            Console.WriteLine($"Training collection: {_trainingCollection}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public Task InsertLog(string message, LogLevel logLevel)
    {
        if (_logCollection == null) throw new NullReferenceException("LogCollection is null");

        var logRecord = new LogRecord { Message = message, Level = logLevel, CreatedAt = DateTime.UtcNow };
        return _logCollection.InsertOneAsync(logRecord);
    }

    public Task InsertTraining(TrainingRecord trainingRecord)
    {
        if (_trainingCollection == null) throw new NullReferenceException("TrainingCollection is null");
        return _trainingCollection.InsertOneAsync(trainingRecord);
    }
    public async Task PartialUpdateTraining(
        Dictionary<string, object> updates,
        ObjectId id)
    {
        if (_trainingCollection == null)
            throw new NullReferenceException("TrainingCollection is null");

        // 1) UpdateDefinition 빌더 준비
        var updateBuilder = Builders<TrainingRecord>.Update;
        var updateDefs = new List<UpdateDefinition<TrainingRecord>>();

        // 2) Dictionary 순회하여 .Set("FieldName", value) 식으로 누적
        foreach (var kvp in updates)
        {
            string fieldName = kvp.Key;   // 예: "BestIteration", "Status", "Accuracy" 등
            object newValue = kvp.Value;  // 예: 10, "Completed", 0.95 등

            // MongoDB에서 "문서.필드"를 표현할 때 점(dot) 표기법 사용 가능
            // ex) "Geometry.MaxRotation" -> geometry 객체 안의 MaxRotation 업데이트
            // 여기서는 kvp.Key를 그대로 사용한다고 가정
            updateDefs.Add(updateBuilder.Set(fieldName, newValue));
        }

        // 3) 여러 Set 연산을 하나로 Combine
        var combinedUpdate = updateBuilder.Combine(updateDefs);

        var filter = Builders<TrainingRecord>.Filter.Eq(x => x.Id, id);
        // 4) UpdateOneAsync 호출
        var result = await _trainingCollection.UpdateOneAsync(filter, combinedUpdate);

        Console.WriteLine($"Matched: {result.MatchedCount}, Modified: {result.ModifiedCount}");
    }
    public Task PushProgressEntry(ObjectId recordId, ProgressHistory newEntry)
    {
        if (_trainingCollection == null)
            throw new NullReferenceException("TrainingCollection is null");

        // 1) Filter: _id == recordId
        var filter = Builders<TrainingRecord>.Filter.Eq(x => x.Id, recordId);

        // 2) Update: progressHistory 배열에 newEntry를 푸시
        var update = Builders<TrainingRecord>.Update
            .Push(x => x.ProgressHistory, newEntry);

        return _trainingCollection.UpdateOneAsync(filter, update);
    }

    public Task UpdateLablesById(ObjectId id, Dictionary<string, float> lables)
    {
        if (_trainingCollection == null) throw new NullReferenceException("TrainingCollection is null");
        var filter = Builders<TrainingRecord>.Filter.Eq(x => x.Id, id);

        var update = Builders<TrainingRecord>.Update.Set(x => x.Lables, lables);

        return _trainingCollection.UpdateOneAsync(filter, update);
    }

}