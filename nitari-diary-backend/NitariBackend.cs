using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using nitari_diary_backend.Entity;
using nitari_diary_backend.Response;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using System.Net;
using System;
using nitari_diary_backend.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Azure.Core;
using Azure;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Web.Http;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Newtonsoft.Json;
using System.IO;
using OpenAI_API;

namespace nitari_diary_backend
{
  public class NitariBackend
  {

    // Specific day's token metrics
    [FunctionName("GetDiaryAll")]
    [OpenApiOperation(operationId: "GetDiaryAll", tags: new[] { "Diary" }, Description = "Get Diary All Entity of Login User")]
    [OpenApiParameter(name: "userId", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The specific userId")]
    [OpenApiParameter(name: "Authorization", In = ParameterLocation.Header, Required = true, Type = typeof(string), Description = "accessToken")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<DiaryResponse>), Description = "The OK response")]
    public static async Task<IActionResult> GetDailySpecificUser(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "diary/all")] HttpRequest req,
    [Table("DailyEntity"/*, Connection = "MyStorage"*/)] TableClient tableClient, ILogger log)
    {
      log.LogInformation($"/diary/all called");
      string userId = req.Query["userId"];

      // API Authrization
      string token = req.Headers["Authorization"].FirstOrDefault();

      var result = await AuthService.VerifyAccessToken(token);
      if (!result)
      {
        return new UnauthorizedResult();
      }

      // Get Diary All Entity of Login User
      var diaries = tableClient.Query<DiaryEntity>().Where(x => x.PartitionKey == userId).ToList();

      List<DiaryResponse> diaryResponses = new List<DiaryResponse>();

      foreach (var diary in diaries)
      {
        DiaryResponse diaryResponse = new DiaryResponse
        {
          UserId = diary.UserId,
          Date = diary.Date,
          Title = diary.Title,
          Description= diary.Description,
          CreatedAt = diary.CreatedAt,
        };
        diaryResponses.Add(diaryResponse);
      };
      return new OkObjectResult(diaryResponses);
    }

    [FunctionName("PostDiary")]
    [OpenApiOperation(operationId: "PostDiary", tags: new[] { "Diary" }, Description = "Post Diary")]
    [OpenApiRequestBody("application/json", typeof(DiaryRequest), Required = true, Description = "Diary data")]
    [OpenApiParameter(name: "Authorization", In = ParameterLocation.Header, Required = true, Type = typeof(string), Description = "accessToken")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(DiaryResponse), Description = "The OK response")]
    public static async Task<IActionResult> PostDailySpecificUser(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "diary")] HttpRequest req,
    [Table("DailyEntity"/*, Connection = "MyStorage"*/)] TableClient tableClient,
    ILogger log)
    {
      log.LogInformation($"[POST] /daily called");
      OpenAPIResponse openApiDiaryResponse = new OpenAPIResponse();

      // API Authrization
      string token = req.Headers["Authorization"].FirstOrDefault();

      var result = await AuthService.VerifyAccessToken(token);
      if (!result)
      {
        return new UnauthorizedResult();
      }

      string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
      var data = JsonConvert.DeserializeObject<DiaryRequest>(requestBody);

      // Get tagDiaries in data and change string
      var tagDiaries = data.tagDiaries;
      // parse tagDiaries to json string
      var tagDiariesJson = JsonConvert.SerializeObject(tagDiaries);

      // Request OpenAI API
      //var openAiResponse = await OpenAiService.GetOpenAiResponse(data.tagDiaries);
      // get an api key.
      string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
      // create an api object.
      int niru = 5;
      string prompt = $"������C����t�H�[�}�b�g��񎦂���̂ŁC���̒ʂ�ɕԎ����s���悤�ɂ��Ă��������D\r\n�܂��C���̃t�H�[�}�b�g�̖ړI�͈ȉ��̒ʂ�ł��D\r\n\r\n�u����̏o�������C��������Ɓ{���̎��Ɏv�������ƁC�łP�Z�b�g�Ƃ��Ď󂯎��C���̓��e�������̗l�q��C�Z�����C���݂̐��_��ԂȂǂ��l�@���āC�ЂƂ̓��L�̕��͂Ƃ��ďo�͂���D�������C�d�x��ADHD���҂炵����������Ƃ₻�̎��v�������ƂɊ֘A����V���������Œ�ł�{niru}�ǉ����C���U�v�l�Ȃ̂����Ă킩��悤�ɏ����Ă��炢�����D�v\r\n\r\n�܂��CInput�Ƀf�[�^�I�u�W�F�N�g��n���܂��D���Ȃ���Outputo�̓��L�̃f�[�^�݂̂�ԐM���Ă��������D\r\n�Ԏ��͂��Ȃ��Č��\�ł��D\r\n\r\n{tagDiariesJson}\r\n\r\n�Ԏ��͂��Ȃ��Č��\�ł��D���L�̃f�[�^�����Ԃ��Ă��������D\r\n�������K�v�Ȃ��ł��D���L�̓��e�ȊO�����L�ڂ��Ȃ��ł�������";
      //apiKey = "sk-xxxxx";
      //OpenAIAPI api = new OpenAIAPI(apiKey);

      //string? diaryFormat = await api.Completions.GetCompletion(prompt);
      //diaryFormat = diaryFormat.Trim();

      //Console.WriteLine(diaryFormat);

      var api = new OpenAIAPI(apiKey);
      var chat = api.Chat.CreateConversation();

      // ChatGPT�Ɏ���
      chat.AppendUserInput(prompt);

      // ChatGPT�̉�
      string response = await chat.GetResponseFromChatbotAsync();

      //openApiDiaryResponse = JsonConvert.DeserializeObject<OpenAPIResponse>(diaryFormat);

      // Create DiaryEntity
      DiaryEntity diaryEntity = new DiaryEntity
      {
        PartitionKey = data.UserId,
        RowKey = data.UserId + data.Date,
        Date = data.Date,
        UserId = data.UserId,
        Title = data.Date,
        Description = response,
        Type = "Diary",
        CreatedAt = DateTime.Now.ToString("yyyyMMddHHmmss")
      };

      // Insert DiaryEntity
      try
      {
        await tableClient.UpdateEntityAsync(diaryEntity, ETag.All, TableUpdateMode.Replace);
      }
      catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
      {
        await tableClient.AddEntityAsync(diaryEntity);
      }

      // Create DiaryResponse
      DiaryResponse diaryResponse = new DiaryResponse
      {
        UserId = data.UserId,
        Title = data.Date,
        Description = response,
        CreatedAt = DateTime.Now.ToString("yyyyMMddHHmmss")
      };

      log.LogInformation($"[POST] /daily called with user={data.UserId}");
      return new OkObjectResult(diaryResponse);
    }

    public class  OpenAPIResponse
    {
      public string Title { get; set; }
      public string Diary { get; set; }
    }

    public class DiaryRequest
    {
      public string UserId { get; set; }
      public List<TagDiary> tagDiaries { get; set; }
      public string Date { get; set; }
    }

    public class TagDiary
    {
      public string activity { get; set; }
      public string feeling { get; set; }
    }

    [FunctionName("Auth")]
    [OpenApiOperation(operationId: "PostDiary", tags: new[] { "Diary" }, Description = "Post Diary")]
    [OpenApiParameter(name: "Authorization", In = ParameterLocation.Header, Required = true, Type = typeof(string), Description = "accessToken")]
    public static async Task<IActionResult> Auth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
      log.LogInformation("C# HTTP trigger function processed a request.");

      // API Authrization
      string token = req.Headers["Authorization"].FirstOrDefault();
      var result = await AuthService.VerifyAccessToken(token);
      if (!result)
      {
        return new UnauthorizedResult();
      }

      return new OkObjectResult(result);
    }
  }
}

