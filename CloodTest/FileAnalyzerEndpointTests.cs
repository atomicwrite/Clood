using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Clood.Endpoints.DTO;
using Newtonsoft.Json;

namespace CloodTest;

[TestFixture]
public class FileAnalyzerEndpointTests
{
    private CloodWebFactory _factory;
    protected HttpClient _client;
    protected string _tempRepoPath;

    [SetUp]
    public void Setup()
    {
        _tempRepoPath = CloodFileMapTestsHelper.GetTempPath();
        if (string.IsNullOrEmpty(_tempRepoPath))
        {
            throw new Exception("Couldn't get temp repo or empty");
        }

        _factory = new CloodWebFactory("https://localhost:9090", _tempRepoPath);


        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("test",
                "true");
            builder.UseSetting("server",
                "true");
            builder.UseSetting("git-root", _tempRepoPath);
            builder.UseSetting("server-urls",
                "https://localhost:9090");
        });
        _client = _factory.CreateClient();

        // Create a temporary folder for the Git repository

        Directory.CreateDirectory(_tempRepoPath);

        // Initialize Git repository
        RunGitCommand($"init {_tempRepoPath}").Wait();
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
        _client.Dispose();

        // Delete the temporary repository
        if (Directory.Exists(_tempRepoPath))
        {
            Directory.Delete(_tempRepoPath, true);
        }
    }

    private async Task RunGitCommand(string arguments)
    {
        try
        {
            var result = await Cli.Wrap("git")
                .WithArguments(arguments)
                .WithWorkingDirectory(_tempRepoPath)
                .ExecuteBufferedAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Git command failed: {result.StandardError}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to run Git command: {ex.Message}", ex);
        }
    }

    [Test]
    public async Task AnalyzeFiles_WithFileOutsideGitRoot_ShouldFail()
    {
        // Arrange
        var outsideFilePath = Path.Combine(CloodFileMapTestsHelper.GetTempPath(),
            "outside_file.cs");
        // await File.WriteAllTextAsync(outsideFilePath,
        //     "public class OutsideClass { }");

        var analyzeRequest = new AnalyzeFilesRequest
        {
            Files = new List<string> { outsideFilePath }
        };

        // Act
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8,
                "application/json"));

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not in the Git root"));
            Assert.That(result.Data, Is.Empty);
        });

   
    }

    [Test]
    public async Task AnalyzeFiles_WithComplexClassStructure_ShouldReturnCorrectSymbolTree()
    {
        // Arrange
        var code = @"
                public class OuterClass
                {
                    public string OuterProperty { get; set; }
                    public void OuterMethod()
                    {
                        var outerVar = 42;
                        void LocalMethod1()
                        {
                            var localMethod1Var = ""test"";
                            void NestedLocalMethod()
                            {
                                var nestedVar = true;
                            }
                        }
                        var anotherOuterVar = 10;
                        void LocalMethod2()
                        {
                            var localMethod2Var = 3.14;
                        }
                    }
                    public static void StaticMethod()
                    {
                        var staticMethodVar = 100;
                        void StaticLocalMethod()
                        {
                            var staticLocalVar = ""static local"";
                        }
                    }
                    private class InnerClass
                    {
                        public int InnerProperty { get; set; }
                        public void InnerMethod()
                        {
                            var innerVar = 1000;
                            void InnerLocalMethod()
                            {
                                var innerLocalVar = ""inner local"";
                            }
                        }
                    }
                }";

        var filePath = Path.Combine(_tempRepoPath,
            "ComplexClass.cs");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest
        {
            Files = [filePath]
        };

        // Act
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8,
                "application/json"));

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
        });

        var expectedStrings = new List<string>
        {
            "ComplexClass.cs:>OuterClass",
            "ComplexClass.cs:>OuterClass@OuterProperty",
            "ComplexClass.cs:>OuterClass/OuterMethod",
            "ComplexClass.cs:>OuterClass/OuterMethod+outerVar",
            "ComplexClass.cs:>OuterClass/OuterMethod/LocalMethod1",
            "ComplexClass.cs:>OuterClass/OuterMethod/LocalMethod1+localMethod1Var",
            "ComplexClass.cs:>OuterClass/OuterMethod/LocalMethod1/NestedLocalMethod",
            "ComplexClass.cs:>OuterClass/OuterMethod/LocalMethod1/NestedLocalMethod+nestedVar",
            "ComplexClass.cs:>OuterClass/OuterMethod/LocalMethod1+nestedVar",
            "ComplexClass.cs:>OuterClass/OuterMethod+anotherOuterVar",
            "ComplexClass.cs:>OuterClass/OuterMethod/LocalMethod2",
            "ComplexClass.cs:>OuterClass/OuterMethod/LocalMethod2+localMethod2Var",
            "ComplexClass.cs:>OuterClass/StaticMethod",
            "ComplexClass.cs:>OuterClass/StaticMethod+staticMethodVar",
            "ComplexClass.cs:>OuterClass/StaticMethod/StaticLocalMethod",
            "ComplexClass.cs:>OuterClass/StaticMethod/StaticLocalMethod+staticLocalVar",
            "ComplexClass.cs:>OuterClass>InnerClass",
            "ComplexClass.cs:>OuterClass>InnerClass@InnerProperty",
            "ComplexClass.cs:>OuterClass>InnerClass/InnerMethod",
            "ComplexClass.cs:>OuterClass>InnerClass/InnerMethod+innerVar",
            "ComplexClass.cs:>OuterClass>InnerClass/InnerMethod/InnerLocalMethod",
            "ComplexClass.cs:>OuterClass>InnerClass/InnerMethod/InnerLocalMethod+innerLocalVar"
        };

        CollectionAssert.AreEqual(expectedStrings, result.Data);
    }

    [Test]
    public async Task AnalyzeFiles_SimpleTypeScriptFile_ShouldReturnCorrectSymbolTree()
    {
        var code = @"
            function greet(name: string): string {
                return `Hello, ${name}!`;
            }
            const user = 'Alice';
            console.log(greet(user));
        ";

        var filePath = Path.Combine(_tempRepoPath, "simple.ts");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest { Files = [filePath] };
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data, Does.Contain("simple.ts:/greet"));
            Assert.That(result.Data, Does.Contain("simple.ts:+user"));
        });
    }

    [Test]
    public async Task AnalyzeFiles_ComplexTypeScriptFile_ShouldReturnCorrectSymbolTree()
    {
        var code = @"
            interface User {
                name: string;
                age: number;
            }

            class UserManager {
                private users: User[] = [];

                constructor() {}

                addUser(user: User): void {
                    this.users.push(user);
                }

                getUser(name: string): User | undefined {
                    return this.users.find(u => u.name === name);
                }
            }

            const manager = new UserManager();
            manager.addUser({ name: 'Alice', age: 30 });
            const foundUser = manager.getUser('Alice');
            console.log(foundUser);
        ";

        var filePath = Path.Combine(_tempRepoPath, "complex.ts");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest { Files = [filePath] };
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
           
            Assert.That(result.Data, Does.Contain("complex.ts:>UserManager"));
            Assert.That(result.Data, Does.Contain("complex.ts:>UserManager@users"));
            Assert.That(result.Data, Does.Contain("complex.ts:>UserManager/addUser"));
            Assert.That(result.Data, Does.Contain("complex.ts:>UserManager/getUser"));
            Assert.That(result.Data, Does.Contain("complex.ts:+manager"));
        });
    }

    [Test]
    public async Task AnalyzeFiles_TypeScriptFileWithDecorators_ShouldReturnCorrectSymbolTree()
    {
        var code = @"
            function log(target: any, key: string, descriptor: PropertyDescriptor) {
                const original = descriptor.value;
                descriptor.value = function(...args: any[]) {
                    console.log(`Calling ${key} with`, args);
                    return original.apply(this, args);
                };
                return descriptor;
            }

            class Calculator {
                @log
                add(a: number, b: number): number {
                    return a + b;
                }

                @log
                subtract(a: number, b: number): number {
                    return a - b;
                }
            }

            const calc = new Calculator();
            calc.add(5, 3);
            calc.subtract(10, 4);
        ";

        var filePath = Path.Combine(_tempRepoPath, "decorators.ts");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest { Files = [filePath] };
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data, Does.Contain("decorators.ts:/log"));
            Assert.That(result.Data, Does.Contain("decorators.ts:>Calculator"));
            Assert.That(result.Data, Does.Contain("decorators.ts:>Calculator/add"));
            Assert.That(result.Data, Does.Contain("decorators.ts:>Calculator/subtract"));
            Assert.That(result.Data, Does.Contain("decorators.ts:+calc"));
        });
    }

    [Test]
    public async Task AnalyzeFiles_TypeScriptFileWithGenerics_ShouldReturnCorrectSymbolTree()
    {
        var code = @"
            interface Repository<T> {
                getAll(): T[];
                save(item: T): void;
            }

            class GenericRepository<T> implements Repository<T> {
                private items: T[] = [];

                getAll(): T[] {
                    return this.items;
                }

                save(item: T): void {
                    this.items.push(item);
                }
            }

            interface User {
                name: string;
                email: string;
            }

            const userRepo = new GenericRepository<User>();
            userRepo.save({ name: 'Alice', email: 'alice@example.com' });
            const allUsers = userRepo.getAll();
        ";

        var filePath = Path.Combine(_tempRepoPath, "generics.ts");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest { Files = [filePath] };
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
       //     Assert.That(result.Data, Does.Contain("generics.ts:>Repository"));
            Assert.That(result.Data, Does.Contain("generics.ts:>GenericRepository"));
            Assert.That(result.Data, Does.Contain("generics.ts:>GenericRepository/getAll"));
            Assert.That(result.Data, Does.Contain("generics.ts:>GenericRepository/save"));
       //     Assert.That(result.Data, Does.Contain("generics.ts:>User"));
            Assert.That(result.Data, Does.Contain("generics.ts:+userRepo"));
        });
    }

    [Test]
    public async Task AnalyzeFiles_TypeScriptFileWithModules_ShouldReturnCorrectSymbolTree()
    {
        var code = @"
            module Geometry {
                export interface Point {
                    x: number;
                    y: number;
                }

                export class Circle {
                    constructor(public center: Point, public radius: number) {}

                    area(): number {
                        return Math.PI * this.radius ** 2;
                    }
                }

                export function distance(p1: Point, p2: Point): number {
                    return Math.sqrt((p2.x - p1.x) ** 2 + (p2.y - p1.y) ** 2);
                }
            }

            const origin: Geometry.Point = { x: 0, y: 0 };
            const unit

Circle = new Geometry.Circle(origin, 1);
            console.log(unit
Circle.area());
            console.log(Geometry.distance(origin, { x: 3, y: 4 }));
        ";

        var filePath = Path.Combine(_tempRepoPath, "modules.ts");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest { Files = [filePath] };
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data, Does.Contain("modules.ts:>Geometry"));
            //Assert.That(result.Data, Does.Contain("modules.ts:>Geometry>Point"));
            Assert.That(result.Data, Does.Contain("modules.ts:>Geometry>Circle"));
            Assert.That(result.Data, Does.Contain("modules.ts:>Geometry>Circle/area"));
            Assert.That(result.Data, Does.Contain("modules.ts:>Geometry/distance"));
            Assert.That(result.Data, Does.Contain("modules.ts:+origin"));
          
        });
    }

    [Test]
    public async Task AnalyzeFiles_SimpleVueFile_ShouldReturnCorrectSymbolTree()
    {
        var code = @"
            <template>
              <div>
                <h1>{{ title }}</h1>
                <button @click='incrementCounter'>Count: {{ counter }}</button>
              </div>
            </template>

            <script>
            export default {
              data() {
                return {
                  title: 'Simple Vue Component',
                  counter: 0
                }
              },
              methods: {
                incrementCounter() {
                  this.counter++
                }
              }
            }
            </script>
        ";

        var filePath = Path.Combine(_tempRepoPath, "simple.vue");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest { Files = [filePath] };
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
           
          //  Assert.That(result.Data, Does.Contain("simple.vue:/div/h1"));
            Assert.That(result.Data, Does.Contain("simple.vue:/div/button/@click=incrementCounter"));
       
         //   Assert.That(result.Data, Does.Contain("simple.vue:data"));
         //   Assert.That(result.Data, Does.Contain("simple.vue:methods/incrementCounter"));
        });
    }

    [Test]
    public async Task AnalyzeFiles_ComplexVueFile_ShouldReturnCorrectSymbolTree()
    {
        var code = @"
        <template>
          <div>
            <h1>{{ title }}</h1>
            <input v-model='newTodo' @keyup.enter='addTodo' />
            <ul>
              <li v-for='todo in filteredTodos' :key='todo.id'>
                <input type='checkbox' v-model='todo.completed' />
                <span :class='{ completed: todo.completed }'>{{ todo.text }}</span>
                <button @click='removeTodo(todo.id)'>Remove</button>
              </li>
            </ul>
            <button @click='clearCompleted'>Clear Completed</button>
          </div>
        </template>

        <script>
        import { ref, computed } from 'vue'

        export default {
          setup() {
            const title = ref('Todo App')
            const newTodo = ref('')
            const todos = ref([])

            const addTodo = () => {
              if (newTodo.value.trim()) {
                todos.value.push({
                  id: Date.now(),
                  text: newTodo.value,
                  completed: false
                })
                newTodo.value = ''
              }
            }

            const removeTodo = (id) => {
              todos.value = todos.value.filter(todo => todo.id !== id)
            }

            const clearCompleted = () => {
              todos.value = todos.value.filter(todo => !todo.completed)
            }

            const filteredTodos = computed(() => todos.value)

            return {
              title,
              newTodo,
              todos,
              addTodo,
              removeTodo,
              clearCompleted,
              filteredTodos
            }
          }
        }
        </script>

        <style scoped>
        .completed {
          text-decoration: line-through;
        }
        </style>
    ";

        var filePath = Path.Combine(_tempRepoPath, "complex.vue");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest { Files = [filePath] };
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
        
            Assert.That(result.Data, Does.Contain("complex.vue:/div/input/v-model=newTodo"));
            Assert.That(result.Data, Does.Contain("complex.vue:/div/input/@keyup.enter=addTodo"));
            Assert.That(result.Data, Does.Contain("complex.vue:/div/ul/li/+v-for=todo in filteredTodos"));
            Assert.That(result.Data, Does.Contain("complex.vue:/div/ul/li/button/@click=removeTodo(todo.id)"));
            Assert.That(result.Data, Does.Contain("complex.vue:/div/button/@click=clearCompleted"));
 
        });
    }

    [Test]
    public async Task AnalyzeFiles_VueFileWithTypescript_ShouldReturnCorrectSymbolTree()
    {
        var code = @"
        <template>
          <div>
            <h1>{{ title }}</h1>
            <p>Count: {{ count }}</p>
            <button @click='increment'>Increment</button>
          </div>
        </template>

        <script lang='ts'>
        import { defineComponent, ref } from 'vue'

        interface User {
          id: number;
          name: string;
        }

        export default defineComponent({
          name: 'TypescriptComponent',
          setup() {
            const title = ref<string>('Typescript in Vue')
            const count = ref<number>(0)
            const user = ref<User>({ id: 1, name: 'John Doe' })

            const increment = () => {
              count.value++
            }

            return {
              title,
              count,
              user,
              increment
            }
          }
        })
        </script>
    ";

        var filePath = Path.Combine(_tempRepoPath, "typescript.vue");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest { Files = [filePath] };
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
      
            Assert.That(result.Data, Does.Contain("typescript.vue:/div/button/@click=increment"));
       
          //  Assert.That(result.Data, Does.Contain("typescript.vue:script/#import=vue"));
            Assert.That(result.Data, Does.Contain("typescript.vue:>User"));
            Assert.That(result.Data, Does.Contain("typescript.vue:/defineComponent"));
            Assert.That(result.Data, Does.Contain("typescript.vue:/defineComponent/setup"));
            Assert.That(result.Data, Does.Contain("typescript.vue:/defineComponent/setup+title"));
            Assert.That(result.Data, Does.Contain("typescript.vue:/defineComponent/setup+count"));
            Assert.That(result.Data, Does.Contain("typescript.vue:/defineComponent/setup+user"));
            Assert.That(result.Data, Does.Contain("typescript.vue:/defineComponent/setup+increment"));
        });
    }

    [Test]
    public async Task AnalyzeFiles_VueFileWithCompositionAPI_ShouldReturnCorrectSymbolTree()
    {
        var code = @"
        <template>
          <div>
            <h1>{{ title }}</h1>
            <p>{{ message }}</p>
            <button @click='updateMessage'>Update Message</button>
          </div>
        </template>

        <script>
        import { ref, computed, onMounted } from 'vue'
        import useCounter from './useCounter'

        export default {
          name: 'CompositionComponent',
          setup() {
            const title = ref('Composition API Example')
            const message = ref('Hello, Vue 3!')

            const { count, increment } = useCounter()

            const uppercaseMessage = computed(() => message.value.toUpperCase())

            const updateMessage = () => {
              message.value = `Updated at ${new Date().toLocaleTimeString()}`
            }

            onMounted(() => {
              console.log('Component mounted')
            })

            return {
              title,
              message,
              count,
              increment,
              uppercaseMessage,
              updateMessage
            }
          }
        }
        </script>
    ";

        var filePath = Path.Combine(_tempRepoPath, "composition.vue");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest { Files = [filePath] };
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var resultDto = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);
        var result = resultDto.Data;
        Assert.That(result, Does.Contain("composition.vue:>default"));

        // Check for component name
        Assert.That(result, Does.Contain("composition.vue:>default@name"));

        // Check for setup function
        Assert.That(result, Does.Contain("composition.vue:>default/setup"));

        // Check for variables in setup
        Assert.That(result, Does.Contain("composition.vue:>default/setup+title"));
        Assert.That(result, Does.Contain("composition.vue:>default/setup+message"));
        Assert.That(result, Does.Contain("composition.vue:>default/setup+count"));
        Assert.That(result, Does.Contain("composition.vue:>default/setup+increment"));
        Assert.That(result, Does.Contain("composition.vue:>default/setup+uppercaseMessage"));
        Assert.That(result, Does.Contain("composition.vue:>default/setup+updateMessage"));

        // Check for lifecycle hook
        Assert.That(result, Does.Contain("composition.vue:>default/setup/onMounted"));

 
 
        Assert.That(result, Does.Contain("composition.vue:>default/setup+title"));
        Assert.That(result, Does.Contain("composition.vue:>default/setup+message"));
        Assert.That(result, Does.Contain("composition.vue:>default/setup+count"));
        Assert.That(result, Does.Contain("composition.vue:>default/setup+increment"));
        Assert.That(result, Does.Contain("composition.vue:>default/setup+uppercaseMessage"));
        Assert.That(result, Does.Contain("composition.vue:>default/setup+updateMessage"));
    }

    [Test]
    public async Task AnalyzeFiles_VueFileWithSetupSugar_ShouldReturnCorrectSymbolTree()
    {
        var code = @"
        <script setup>
        import { ref, computed } from 'vue'

        const count = ref(0)
        const double = computed(() => count.value * 2)

        function increment() {
          count.value++
        }
        </script>

        <template>
          <button @click='increment'>Count is: {{ count }}</button>
          <p>Double is: {{ double }}</p>
        </template>
    ";

        var filePath = Path.Combine(_tempRepoPath, "setup-sugar.vue");
        await File.WriteAllTextAsync(filePath, code);

        var analyzeRequest = new AnalyzeFilesRequest { Files = [filePath] };
        var response = await _client.PostAsync("/api/clood/analyze-files",
            new StringContent(JsonConvert.SerializeObject(analyzeRequest), Encoding.UTF8, "application/json"));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CloodResponse<List<string>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
      
            //Assert.That(result.Data, Does.Contain("setup-sugar.vue:script/#import=vue"));
            Assert.That(result.Data, Does.Contain("setup-sugar.vue:+count"));
            Assert.That(result.Data, Does.Contain("setup-sugar.vue:+double"));
            Assert.That(result.Data, Does.Contain("setup-sugar.vue:/increment"));
;
        });
    }
}