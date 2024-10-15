using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace CloodTest;

[TestFixture]
public class VueTemplateAnalyzerTests
{
  private VueTemplateAnalyzer _analyzer;

  [SetUp]
  public void Setup()
  {
    _analyzer = new VueTemplateAnalyzer();
  }

  [Test]
  public void TestSimpleTemplate()
  {
    var template = @"
<template>
  <div v-if='showDiv'>
    <input v-model='message' />
    <button @click='sendMessage'>Send</button>
  </div>
</template>";

    var result = _analyzer.AnalyzeTemplate(template);

    Assert.That(result, Has.Count.EqualTo(6));
    Assert.That(result, Contains.Item("/div"));
    Assert.That(result, Contains.Item("/div/+v-if=showDiv"));
    Assert.That(result, Contains.Item("/div/input"));
    Assert.That(result, Contains.Item("/div/input/v-model=message"));
    Assert.That(result, Contains.Item("/div/button"));
    Assert.That(result, Contains.Item("/div/button/@click=sendMessage"));
  }

  [Test]
  public void TestDeeplyNestedTemplate()
  {
    var template = @"
<template>
  <div>
    <nav>
      <ul>
        <li v-for='item in items'>
          <a :href='item.url' @click='navigate(item)'>{{ item.text }}</a>
        </li>
      </ul>
    </nav>
    <main>
      <article>
        <section v-if='content'>
          <h1>{{ content.title }}</h1>
          <p v-html='content.body'></p>
        </section>
      </article>
    </main>
  </div>
</template>";

    var result = _analyzer.AnalyzeTemplate(template);

    Assert.That(result, Has.Count.EqualTo(9));
    Assert.That(result, Contains.Item("/div/nav/ul/li"));
    Assert.That(result, Contains.Item("/div/nav/ul/li/+v-for=item in items"));
    Assert.That(result, Contains.Item("/div/nav/ul/li/a/:href=item.url"));
    Assert.That(result, Contains.Item("/div/nav/ul/li/a/@click=navigate(item)"));
    Assert.That(result, Contains.Item("/div/main/article/section"));
    Assert.That(result, Contains.Item("/div/main/article/section/+v-if=content"));
    Assert.That(result, Contains.Item("/div/main/article/section/p/+v-html=content.body"));
    Assert.That(result, Contains.Item("/div/nav/ul/li/a"));
    Assert.That(result, Contains.Item("/div/main/article/section/p"));
  }

  [Test]
  public void TestComplexBindings()
  {
    var template = @"
<template>
  <div>
    <component 
      :is='dynamicComponent'
      :class='{ active: isActive, ""error"": hasError }'
      :style='{ color: textColor, fontSize: size + ""px"" }'
      v-bind='$attrs'
      v-on='$listeners'
    >
    </component>
  </div>
</template>";

    var result = _analyzer.AnalyzeTemplate(template);

    Assert.That(result, Has.Count.EqualTo(6));
    Assert.That(result, Contains.Item("/div/component"));
    Assert.That(result, Contains.Item("/div/component/:is=dynamicComponent"));
    Assert.That(result, Contains.Item("/div/component/:class={ active: isActive, \"error\": hasError }"));
    Assert.That(result, Contains.Item("/div/component/:style={ color: textColor, fontSize: size + \"px\" }"));
    Assert.That(result, Contains.Item("/div/component/+v-bind=$attrs"));
    Assert.That(result, Contains.Item("/div/component/+v-on=$listeners"));
  }

  [Test]
  public void TestCustomDirectives()
  {
    var template = @"
<template>
  <div>
    <input v-focus v-model='searchText' />
    <p v-highlight:argument.modifier='color'>Highlighted text</p>
    <span v-custom-directive='{ prop1: value1, prop2: value2 }'>Custom directive</span>
  </div>
</template>";

    var result = _analyzer.AnalyzeTemplate(template);

    Assert.That(result, Has.Count.EqualTo(7));
    Assert.That(result, Contains.Item("/div/input"));
    Assert.That(result, Contains.Item("/div/input/+v-focus"));
    Assert.That(result, Contains.Item("/div/input/v-model=searchText"));
    Assert.That(result, Contains.Item("/div/p"));
    Assert.That(result, Contains.Item("/div/p/+v-highlight:argument.modifier=color"));
    Assert.That(result, Contains.Item("/div/span"));
    Assert.That(result, Contains.Item("/div/span/+v-custom-directive={ prop1: value1, prop2: value2 }"));
  }

  [Test]
  public void TestSlots()
  {
    var template = @"
<template>
  <base-layout>
    <template v-slot:header='slotProps'>
      <h1>{{ slotProps.title }}</h1>
    </template>
    <template #default>
      <p>Default slot content</p>
    </template>
    <template v-slot:footer='{ copyright }'>
      <p>{{ copyright }}</p>
    </template>
  </base-layout>
</template>";

    var result = _analyzer.AnalyzeTemplate(template);

    Assert.That(result, Has.Count.EqualTo(3));
    Assert.That(result, Contains.Item("/base-layout/template"));
    Assert.That(result, Contains.Item("/base-layout/template/+v-slot:header=slotProps"));
    Assert.That(result, Contains.Item("/base-layout/template/+v-slot:footer={ copyright }"));
  }

  [Test]
  public void TestEmptyTemplate()
  {
    var template = "<template></template>";

    var result = _analyzer.AnalyzeTemplate(template);

    Assert.That(result, Is.Empty);
  }

  [Test]
  public void TestTemplateWithOnlyStaticContent()
  {
    var template = @"
<template>
  <div class='static'>
    <p>This is static content</p>
    <span>No Vue-specific attributes here</span>
  </div>
</template>";

    var result = _analyzer.AnalyzeTemplate(template);

    Assert.That(result, Is.Empty);
  }
}